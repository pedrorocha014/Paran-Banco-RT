namespace CustomerWebApi.Controllers.Dto;

public record CreateCustomerRequestDto
{
    public string Name { get; set; } = string.Empty;
}

public record CreateCustomerResponseDto
{
    public Guid Id { get; set; }
}


