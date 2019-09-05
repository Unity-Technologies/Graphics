/**
 * TestRail API binding for .NET (API v2, available since TestRail 3.0)
 *
 * Learn more:
 *
 * http://docs.gurock.com/testrail-api2/start
 * http://docs.gurock.com/testrail-api2/accessing
 *
 * Copyright Gurock Software GmbH. See license.md for details.
 */

using System;
using System.Runtime.Serialization;

namespace Gurock.TestRail
{
	[Serializable]
	public class APIException: Exception
	{
		public APIException()
		{
		}

		public APIException(string message): base(message)
		{
		}

		public APIException(string message,
			Exception innerException): base(message, innerException)
		{
		}

		protected APIException(SerializationInfo info,
			StreamingContext context): base(info, context)
		{
		}
	}
}
