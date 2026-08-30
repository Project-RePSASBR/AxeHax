using AxeHax.Core;
using AxeHax.Core.Logging;
using NotEnoughLogs.Sinks;

namespace AxeHax;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        State.InitializeLogger([new ConsoleSink(), new EventSink()]);

        State.Logger.LogInfo(
            LogType.OSIntegration, 
            $"AxeHax launched with args [{string.Join(',', args)}] (count: {args.Length})"
        );
        //bool isCliInvocation = (
        //    args.Length > 0 
        //    && !UrlAssociationHandler.IsArgsUrl(args) 
        //    && !UrlAssociationHandler.IsArgsTryingToRegisterAssociations(args)
        //);
    }
}
