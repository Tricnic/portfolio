using PlayFab;
using System.Text;
using UnityEngine;

namespace BSCore
{
    public class PlayFabService
    {
        protected System.Action<PlayFabError> OnFailureCallback(System.Action retry, System.Action<FailureReasons> callback)
        {
            var timeout = new BackoffTimeout();
            return error =>
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Playfab Error: {error.Error}");
                sb.AppendLine($"Message: {error.ErrorMessage}");
                sb.AppendLine($"API: {error.ApiEndpoint}");
                sb.AppendLine($"HttpCode: {error.HttpCode}");
                sb.AppendLine($"HttpStatus: {error.HttpStatus}");

                var customDataLog = error.CustomData == null ? "Null" : error.CustomData.ToString();
                sb.AppendLine($"CustomData: {customDataLog}");

                if(error.ErrorDetails == null || error.ErrorDetails.Count <= 0)
                {
                    sb.AppendLine($"ErrorDetails: Null or Empty");
                }
                else
                {
                    sb.AppendLine("Error Details:");
                    foreach(var e in error.ErrorDetails)
                    {
                        sb.AppendLine($"Key: {e.Key} | Value: {e.Value}");
                    }
                }

                Debug.Log(sb.ToString());

                FailureReasons reason = Utils.PlayFabUtils.ParseFailureReason(error);
                if (reason != FailureReasons.WebTimeout || !timeout.RunAfterBackoff(retry))
                {
                    Debug.LogErrorFormat("[ServiceError] {0}: {1}", error.Error, error.ErrorMessage);
                    callback(reason);
                }
            };
        }
    }
}
