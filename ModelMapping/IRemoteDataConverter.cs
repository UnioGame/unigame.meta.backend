namespace Game.Modules.ModelMapping
{
    using System;

    public interface IRemoteDataConverter
    {
        public Object Convert(Type type, string data);
    }
}