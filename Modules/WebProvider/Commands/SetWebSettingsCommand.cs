namespace VN
{
    using Modules.WebServer;
    using Sirenix.OdinInspector;
    using UniGame.UniBuild.Editor.ClientBuild.Interfaces;
    using UniModules.Editor;
    using UniModules.UniGame.UniBuild.Editor.ClientBuild.Commands.PreBuildCommands;

    public class SetWebSettingsCommand : SerializableBuildCommand
    {
        public string CurrentUrl;
        public override void Execute(IUniBuilderConfiguration buildParameters)
        {
            Execute();
        }

        [Button]
        public void Execute()
        {
            var webSettings = AssetEditorTools.GetAsset<WebMetaProviderAsset>();
            webSettings.settings.defaultUrl = CurrentUrl;
            webSettings.MarkDirty();
        }
    }
}
