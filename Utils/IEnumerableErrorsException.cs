using System;
using System.Collections.Generic;

namespace WebApi.Utils
{
    public class EnumerableErrorsException<T> : Exception
    {
        public readonly IEnumerable<T> Errors;

        public EnumerableErrorsException(IEnumerable<T> errors)
        {
            Errors = errors;
        }
    }
}