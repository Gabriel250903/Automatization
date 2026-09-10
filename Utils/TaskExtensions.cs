using Automatization.Services;

namespace Automatization.Utils
{
    public static class TaskExtensions
    {
        public static void SafeFireAndForget(
            this Task task,
            string? contextName = null,
            Action<Exception>? onException = null
        )
        {
            if (task == null)
            {
                return;
            }

            _ = task.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        Exception ex = t.Exception.Flatten().InnerException ?? t.Exception;
                        if (onException != null)
                        {
                            try
                            {
                                onException(ex);
                            }
                            catch (Exception handlerEx)
                            {
                                LogService.LogError(
                                    $"Exception handler failed in SafeFireAndForget ({contextName ?? "Unknown"}): {handlerEx.Message}",
                                    handlerEx
                                );
                            }
                        }
                        else
                        {
                            LogService.LogError(
                                $"Unhandled exception in background task ({contextName ?? "Unknown"}): {ex.Message}",
                                ex
                            );
                        }
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously
            );
        }
    }
}
