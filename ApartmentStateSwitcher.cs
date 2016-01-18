// Stephen Toub
// stoub@microsoft.com

using System;
using System.Threading;
using System.Reflection;

namespace Durrant.Common
{
	/// <summary>Enables easy execution of code in guaranteed apartment states.</summary>
	public sealed class ApartmentStateSwitcher
	{
		/// <summary>The delegate to be invoked.</summary>
		private volatile Delegate _delegate;
		/// <summary>The parameters to use during invocation.</summary>
		private volatile object[] _parameters;
		/// <summary>Any execption thrown from the invocation.</summary>
		private volatile Exception _exc;
		/// <summary>The return value from the invocation.</summary>
		private volatile object _rv;

		/// <summary>Prevent external instantation.</summary>
		private ApartmentStateSwitcher(){}

		/// <summary>Runs the target delegate, capturing the results.</summary>
		private void Run()
		{
			try { _rv = _delegate.DynamicInvoke(_parameters); }
			catch(MemberAccessException exc) { _exc = exc; }
			catch(TargetException exc) { _exc = exc; }
			catch(TargetInvocationException exc) { _exc = exc; }
		}

		/// <summary>Invokes the specified delegate with the specified parameters in the specified kind of apartment state.</summary>
		/// <param name="d">The delegate to be invoked.</param>
		/// <param name="parameters">The parameters to pass to the delegate being invoked.</param>
		/// <param name="state">The apartment state to run under.</param>
		/// <returns>The result of calling the delegate.</returns>
		public static object Execute(
			Delegate d, object[] parameters, ApartmentState state)
		{
			if (d == null) throw new ArgumentNullException("d");
			if (state != ApartmentState.MTA && state != ApartmentState.STA)
				throw new ArgumentOutOfRangeException("state");

			if (Thread.CurrentThread.ApartmentState == state)
			{
				return d.DynamicInvoke(parameters);
			}
			else
			{
				ApartmentStateSwitcher switcher = new ApartmentStateSwitcher();
				switcher._delegate = d;
				switcher._parameters = parameters;

				Thread t = new Thread(new ThreadStart(switcher.Run));
				t.ApartmentState = state;
				t.IsBackground = true;
				t.Start();
				t.Join();

				if (switcher._exc != null) throw switcher._exc;
				return switcher._rv;
			}
		}
	}
}
