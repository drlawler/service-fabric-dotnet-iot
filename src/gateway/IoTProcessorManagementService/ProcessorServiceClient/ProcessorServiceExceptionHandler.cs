﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace IoTProcessorManagementService
{
    internal class ProcessorServiceExceptionHandler : IExceptionHandler
    {
        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            if (exceptionInformation.Exception is TimeoutException)
            {
                result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                return true;
            }
            else if (exceptionInformation.Exception is ProtocolViolationException)
            {
                result = new ExceptionHandlingThrowResult();
                return true;
            }
            else if (exceptionInformation.Exception is WebException)
            {
                WebException we = exceptionInformation.Exception as WebException;
                HttpWebResponse errorResponse = we.Response as HttpWebResponse;

                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        // This could either mean we requested an endpoint that does not exist in the service API (a user error)
                        // or the address that was resolved by fabric client is stale (transient runtime error) in which we should re-resolve.
                        result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                        return true;
                    }

                    if (errorResponse.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        // The address is correct, but the server processing failed.
                        // Retry the operation without re-resolving the address.
                        result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, true, retrySettings, retrySettings.DefaultMaxRetryCount);
                        return true;
                    }
                }

                if (we.Status == WebExceptionStatus.Timeout ||
                    we.Status == WebExceptionStatus.RequestCanceled ||
                    we.Status == WebExceptionStatus.ConnectionClosed ||
                    we.Status == WebExceptionStatus.ConnectFailure)
                {
                    result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
