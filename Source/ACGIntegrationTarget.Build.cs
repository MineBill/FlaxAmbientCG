using Flax.Build;

public class ACGIntegrationTarget : GameProjectTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for game
        Modules.Add("ACGIntegration");
    }
}
