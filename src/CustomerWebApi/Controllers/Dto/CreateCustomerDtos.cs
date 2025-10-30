namespace CustomerWebApi.Controllers.Dto;

public class CreateCustomerRequestDto
{
    public string Name { get; set; } = string.Empty;
}

public class CreateCustomerResponseDto
{
    public Guid Id { get; set; }
}


