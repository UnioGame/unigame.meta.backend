namespace UniGame.MetaBackend.Runtime.Contracts
{
    using System;

    [Serializable]
    public class NakamaDeviceIdAuthContract : NakamaContract<NakamaDeviceIdAuthenticateData,NakamaServiceResult>,INakamaAuthContract
    {
        public NakamaDeviceIdAuthenticateData authData = new();

        public override string Path => "nakama_device_id_auth";

        public override object Payload => authData;

        public string AuthTypeName => authData.AuthTypeName;

        public INakamaAuthenticateData AuthData => authData;
    }
}