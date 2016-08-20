namespace Zen.Trunk
{
	public class TraceableObject
	{
		private ITracer _tracer;

		#region Protected Properties
		/// <summary>
		/// Gets the diagnostics tracer object.
		/// </summary>
		/// <value>The tracer.</value>
		protected ITracer Tracer
		{
			get
			{
				if (_tracer == null)
				{
					_tracer = CreateTracer(TracerName);
				}
				return _tracer;
			}
		}

		/// <summary>
		/// Gets the name of the tracer.
		/// </summary>
		/// <value>The name of the tracer.</value>
		protected virtual string TracerName
		{
			get
			{
				return GetType().Name;
			}
		}
		#endregion

		#region Protected Methods
		protected virtual ITracer CreateTracer(string tracerName)
		{
			return TS.CreateCoreTracer(tracerName);
		}
		#endregion
	}
}
