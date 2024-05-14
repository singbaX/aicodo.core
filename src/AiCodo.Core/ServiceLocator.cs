using System;

namespace AiCodo
{
    public class ServiceLocator
    {
        public static IServiceLocator Current
        {
            get; set;
        }
    }

    public interface IServiceLocator
    {
        bool IsRegistered<T>();

        bool IsRegistered<T>(string name);

        T Get<T>();

        bool TryGet<T>(out T value);

        T GetNamed<T>(string name);

        bool TryGetNamed<T>(string name, out T value);
    }
}
