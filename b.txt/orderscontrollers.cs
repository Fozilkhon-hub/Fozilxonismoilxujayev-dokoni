using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    // 1. Buyurtma yaratish (Checkout) (POST: api/orders)
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        // Foydalanuvchining savatini topamiz
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == dto.UserId);

        if (cart == null || !cart.Items.Any())
        {
            return BadRequest("Savat bo'sh, buyurtma berib bo'lmaydi.");
        }

        // Yangi buyurtma ochamiz
        var order = new Order
        {
            UserId = dto.UserId,
            OrderDate = DateTime.UtcNow,
            Status = "Pending"
        };

        decimal totalAmount = 0;

        foreach (var cartItem in cart.Items)
        {
            if (cartItem.Product != null)
            {
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Product.Price
                };

                totalAmount += cartItem.Product.Price * cartItem.Quantity;
                order.OrderItems.Add(orderItem);
            }
        }

        order.TotalAmount = totalAmount;

        // Bazaga buyurtmani qo'shamiz
        _context.Orders.Add(order);

        // Savatni tozalaymiz
        _context.CartItems.RemoveRange(cart.Items);

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Buyurtma muvaffaqiyatli rasmiylashtirildi!", OrderId = order.Id, TotalAmount = totalAmount });
    }

    // 2. Foydalanuvchining buyurtmalarini ko'rish (GET: api/orders/user123)
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserOrders(string userId)
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.UserId == userId)
            .ToListAsync();

        return Ok(orders);
    }
}
