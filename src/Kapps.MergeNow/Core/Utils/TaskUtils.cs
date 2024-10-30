﻿using System;
using System.Threading.Tasks;

namespace MergeNow.Core.Utils
{
    public static class TaskUtils
    {
#pragma warning disable S3168 // "async" methods should not return "void"
#pragma warning disable VSTHRD100 // Avoid async void methods
        public static async void FireAsyncCatchErrors(this Task task, Action<Exception> errorHandler)
#pragma warning restore VSTHRD100 // Avoid async void methods
#pragma warning restore S3168 // "async" methods should not return "void"

        {
            if (task == null)
            {
                return;
            }

            try
            {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                await task;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }

        public static T ReturnAsyncCatchErrors<T>(this Task<T> task, Action<Exception> errorHandler)
        {
            if (task == null)
            {
                return default;
            }

            try
            {
                return task.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
                return default;
            }
        }
    }
}
