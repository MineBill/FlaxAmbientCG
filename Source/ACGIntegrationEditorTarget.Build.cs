using Flax.Build;

public class ACGIntegrationEditorTarget : GameProjectEditorTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for editor
        Modules.Add("ACGIntegrationEditor");
        Modules.Add("ACGIntegration");
    }
}
