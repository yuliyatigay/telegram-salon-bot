namespace Domain.Models;

public class Procedure
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; }
}