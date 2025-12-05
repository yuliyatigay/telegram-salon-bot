namespace Domain.Models;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string PhoneNumber { get; set; }
    public string TelegramId { get; set; }
    public virtual List<Procedure> Procedures { get; set; }
}