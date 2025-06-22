namespace Pangolin.Utility.Aes;

public class AesFactory
{
    private static Dictionary<string, IAesHandler> _handlers = new Dictionary<string, IAesHandler>();

    public static IAesHandler GetHandler(string name)
    {
        if (_handlers.TryGetValue(name, out var result))
        {
            return result;
        }

        lock (_handlers)
        {
            if (_handlers.TryGetValue(name, out result))
            {
                return result;
            }

            switch (name)
            {
                case CbcAesHandler.Name:
                {
                    IAesHandler handler = new CbcAesHandler();
                    _handlers.Add(CbcAesHandler.Name, handler);
                    result = handler;
                    break;
                }
                case CfbAesHandler.Name:
                {
                    _handlers.Add(CfbAesHandler.Name, new CfbAesHandler());
                    break;
                }


                default:
                    result = null;
                    break;
            }
        }

        return result;
    }
}