using Application.Abstractions;
using Application.UseCases.Cards;
using Application.UseCases.Customers;
using Application.UseCases.Proposals;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateCustomerUseCase, CreateCustomerUseCase>();
        services.AddScoped<ICreateProposalUseCase, CreateProposalUseCase>();
        services.AddScoped<ICreateCardUseCase, CreateCardUseCase>();
        return services;
    }
}


