using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace Nouz.Architecture.Tests;

public class LayerTests
{
    private static readonly string ApplicationLayer = "Nouz.Application";
    private static readonly Assembly ApplicationAssembly = Assembly.LoadFrom($"{ApplicationLayer}.dll");

    private static readonly string DomainLayer = "Nouz.Domain";
    private static readonly Assembly DomainAssembly = Assembly.LoadFrom($"{DomainLayer}.dll");

    private static readonly string InfrastructureLayer = "Nouz.Infrastructure";
    private static readonly Assembly InfrastructureAssembly = Assembly.LoadFrom($"{InfrastructureLayer}.dll");

    [Fact]
    public void ApplicationLayer_ShouldNotAccessInfrastructureLayer()
    {
        // Arrange
        var rule = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureLayer);

        // Act
        var result = rule.GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainLayer_ShouldNotAccessInfrastructureLayer()
    {
        // Arrange
        var rule = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureLayer);

        // Act
        var result = rule.GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void DomainLayer_ShouldNotAccessApplicationLayer()
    {
        // Arrange
        var rule = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationLayer);

        // Act
        var result = rule.GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void InfrastructureLayer_ShouldNotBeUsedInAnyOtherLayerExceptForServiceRegistrations()
    {
        // Arrange
        var rule = Types.InAssembly(InfrastructureAssembly)
            .That()
            .DoNotHaveName("ServiceCollectionExtensions")
            .ShouldNot()
            .BePublic();

        // Act
        var result = rule.GetResult();

        // Assert
        result.IsSuccessful.ShouldBeTrue();
    }
}