using System;
using contoso.ado.EventListeners;

namespace contoso.ado
{
	/// <summary>Static management of event listener configuration.</summary>
	public static class DataModuleEvents
	{
		/// <summary>Set the client event listener once per application domain to initialize the logging infrastructure.</summary>
		public static void ObserveWith(IDataModuleObserver listener) 
		{
			ContextLog.Q.SetListener(listener);
		}
	}
}
