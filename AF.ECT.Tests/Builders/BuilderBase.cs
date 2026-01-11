namespace AF.ECT.Tests.Builders;

/// <summary>
/// Base class for test data builders using the Builder pattern.
/// Provides common functionality for all test data builders in the solution.
/// Enables fluent, readable test data creation with sensible defaults.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type (enables fluent interface)</typeparam>
/// <typeparam name="TProduct">The type being built</typeparam>
public abstract class BuilderBase<TBuilder, TProduct>
    where TBuilder : BuilderBase<TBuilder, TProduct>
{
    /// <summary>
    /// Gets the concrete builder instance for fluent interface chaining.
    /// </summary>
    protected TBuilder Self => (TBuilder)this;

    /// <summary>
    /// Builds and returns the product instance.
    /// </summary>
    /// <returns>The constructed product with all configured values</returns>
    public abstract TProduct Build();

    /// <summary>
    /// Creates a new builder instance with default values.
    /// </summary>
    /// <returns>A new builder instance</returns>
    protected static TBuilder CreateNew() => (Activator.CreateInstance<TBuilder>())!;

    /// <summary>
    /// Clones the current builder state to create an independent copy.
    /// Useful for creating variations of existing builders without side effects.
    /// </summary>
    /// <returns>A new builder with the same configuration</returns>
    public virtual TBuilder Clone()
    {
        var clone = CreateNew();
        CopyStateTo(clone);
        return clone;
    }

    /// <summary>
    /// Copies the builder state to another builder instance.
    /// Override in derived classes to copy custom properties.
    /// </summary>
    /// <param name="target">The target builder to copy state to</param>
    protected virtual void CopyStateTo(TBuilder target)
    {
        // Override in derived classes to copy custom properties
    }
}
