using Azure;
using CoffeeNChillGateway.Models;
using CoffeeNChillGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers
{
    /// <summary>
    /// Orders. Students can place an order and check on it without logging in.
    /// Staff change the order status and see all orders.
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private QueueStorageService _queueService;
        private OrderTableService _orderService;
        private ILogger<OrdersController> _logger;

        public OrdersController(
            QueueStorageService queueService,
            OrderTableService orderService,
            ILogger<OrdersController> logger)
        {
            _queueService = queueService;
            _orderService = orderService;
            _logger = logger;
        }

        /// <summary>
        /// Places an order on the queue. No login needed.
        /// </summary>
        /// <remarks>
        /// The order is checked and put on order-processing-queue, and the answer comes back straight
        /// away (202). A background function then moves it from Received to Preparing to Ready.
        /// OrderId and OrderTimestamp are optional. If you leave them out, they are made for you.
        /// </remarks>
        /// <param name="order">The order.</param>
        /// <response code="202">The order was accepted and put on the queue.</response>
        /// <response code="400">The order is not valid. The response lists every problem.</response>
        /// <response code="409">An order with this OrderId already exists.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpPost("queue")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> QueueOrder([FromBody] OrderMessage order)
        {
            // Fill in OrderId and OrderTimestamp if they were left out, then check every field
            order.ApplyDefaults();
            List<string> errors = order.Validate();
            if (errors.Count > 0)
            {
                return BadRequest(new { Message = "Order validation failed.", Errors = errors });
            }

            try
            {
                await _orderService.EnsureTablesExistAsync();

                OrderEntity existingOrder = _orderService.GetOrder(order.OrderId);
                if (existingOrder != null)
                {
                    return Conflict(new
                    {
                        Message = "An order with OrderId " + order.OrderId + " already exists. Its status is " + existingOrder.Status + "."
                    });
                }

                QueueSendResult result = await _queueService.SendOrderAsync(order);

                _logger.LogInformation("Order {OrderId} for {CustomerName} placed on the queue as message {MessageId}.",
                    order.OrderId, order.CustomerName, result.MessageId);

                return Accepted(new
                {
                    Message = "Order accepted and placed on the queue.",
                    OrderId = order.OrderId,
                    QueueName = QueueStorageService.OrderQueueName,
                    MessageId = result.MessageId,
                    Status = "Queued",
                    Order = order,
                    Base64Message = result.Base64Message
                });
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage error while queueing order {OrderId}.", order.OrderId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not reach the storage, so the order was not queued." });
            }
        }

        /// <summary>
        /// Gets one order and its current status. No login needed, so students can follow their order.
        /// </summary>
        /// <param name="orderId">The order id, for example "ORD-2026-8801".</param>
        /// <response code="200">The order.</response>
        /// <response code="404">There is no order with this id. If you only just placed it, the queue may not have processed it yet.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpGet("{orderId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetOrderById(string orderId)
        {
            try
            {
                await _orderService.EnsureTablesExistAsync();
                OrderEntity order = _orderService.GetOrder(orderId);

                if (order == null)
                {
                    return NotFound(new
                    {
                        Message = "Order " + orderId + " was not found. If you only just placed it, the queue may not have processed it yet."
                    });
                }

                return Ok(OrderDto.FromEntity(order));
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage error while reading order {OrderId}.", orderId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not read the order from the storage." });
            }
        }

        /// <summary>
        /// Gets all orders. Admin and Staff only.
        /// </summary>
        /// <response code="200">The list of orders.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                await _orderService.EnsureTablesExistAsync();
                List<OrderEntity> orders = _orderService.GetAllOrders();

                List<OrderDto> dtoList = new List<OrderDto>();
                for (int i = 0; i < orders.Count; i++)
                {
                    dtoList.Add(OrderDto.FromEntity(orders[i]));
                }

                return Ok(dtoList);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage error while reading orders.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not read orders from the storage." });
            }
        }

        /// <summary>
        /// Moves an order to a new status. Admin and Staff only.
        /// </summary>
        /// <remarks>
        /// A status can only move forward (Received, Preparing, Ready, Collected). An order can
        /// only be Collected when it is Ready.
        /// </remarks>
        /// <param name="dto">The order id and the new status.</param>
        /// <response code="200">The order with its new status.</response>
        /// <response code="400">The status is not one of the four allowed statuses.</response>
        /// <response code="404">There is no order with this id.</response>
        /// <response code="409">The status cannot move like that.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpPut("status")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDto dto)
        {
            string newStatus = NormaliseStatus(dto.Status);
            if (newStatus == null)
            {
                return BadRequest(new { Message = "Status must be Received, Preparing, Ready or Collected." });
            }

            string orderId = dto.OrderId.Trim();

            try
            {
                await _orderService.EnsureTablesExistAsync();
                OrderEntity order = _orderService.GetOrder(orderId);

                if (order == null)
                {
                    return NotFound(new { Message = "Order " + orderId + " was not found." });
                }

                int currentRank = GetStatusRank(order.Status);
                int newRank = GetStatusRank(newStatus);

                if (newRank <= currentRank)
                {
                    return Conflict(new
                    {
                        Message = "Order " + orderId + " is already " + order.Status + ". A status can only move forward."
                    });
                }

                if (newStatus == OrderStatus.Collected && order.Status != OrderStatus.Ready)
                {
                    return Conflict(new
                    {
                        Message = "Order " + orderId + " is still " + order.Status + ". It can only be collected once it is Ready."
                    });
                }

                string oldStatus = order.Status;
                await _orderService.UpdateOrderStatusAsync(order, newStatus);
                _logger.LogInformation("Order {OrderId} moved from {OldStatus} to {NewStatus}.", orderId, oldStatus, newStatus);

                return Ok(OrderDto.FromEntity(order));
            }
            catch (RequestFailedException ex)
            {
                // 412 means the order changed between our read and our update (the queue function may have moved it)
                if (ex.Status == 412)
                {
                    return Conflict(new { Message = "Order " + orderId + " changed while it was being updated. Please try again." });
                }

                _logger.LogError(ex, "Azure Storage error while updating order {OrderId}.", orderId);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not update the order in the storage." });
            }
        }

        /// <summary>
        /// Lists the orders that failed 3 times and ended up on the poison queue. Admin and Staff only.
        /// </summary>
        /// <response code="200">The list of failed orders.</response>
        /// <response code="503">The storage is not reachable.</response>
        [HttpGet("~/api/failed-orders")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(List<FailedOrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetFailedOrders()
        {
            try
            {
                await _orderService.EnsureTablesExistAsync();
                List<FailedOrderEntity> failedOrders = _orderService.GetFailedOrders();

                List<FailedOrderDto> dtoList = new List<FailedOrderDto>();
                for (int i = 0; i < failedOrders.Count; i++)
                {
                    dtoList.Add(FailedOrderDto.FromEntity(failedOrders[i]));
                }

                return Ok(dtoList);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Storage error while reading failed orders.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { Message = "Could not read failed orders from the storage." });
            }
        }

        /// <summary>
        /// Gives the status its correct spelling, or null if it is not one of the four statuses.
        /// </summary>
        private string NormaliseStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            string cleanStatus = status.Trim();

            if (string.Equals(cleanStatus, OrderStatus.Received, StringComparison.OrdinalIgnoreCase))
            {
                return OrderStatus.Received;
            }
            else if (string.Equals(cleanStatus, OrderStatus.Preparing, StringComparison.OrdinalIgnoreCase))
            {
                return OrderStatus.Preparing;
            }
            else if (string.Equals(cleanStatus, OrderStatus.Ready, StringComparison.OrdinalIgnoreCase))
            {
                return OrderStatus.Ready;
            }
            else if (string.Equals(cleanStatus, OrderStatus.Collected, StringComparison.OrdinalIgnoreCase))
            {
                return OrderStatus.Collected;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gives each status a number so we can tell if a change moves forward.
        /// </summary>
        private int GetStatusRank(string status)
        {
            if (status == OrderStatus.Received)
            {
                return 1;
            }
            else if (status == OrderStatus.Preparing)
            {
                return 2;
            }
            else if (status == OrderStatus.Ready)
            {
                return 3;
            }
            else if (status == OrderStatus.Collected)
            {
                return 4;
            }
            else
            {
                return 0;
            }
        }
    }
}
