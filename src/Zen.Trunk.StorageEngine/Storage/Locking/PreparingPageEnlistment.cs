namespace Zen.Trunk.Storage.Locking
{
	using System;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Zen.Trunk.Storage.Locking.PageEnlistment" />
    public abstract class PreparingPageEnlistment : PageEnlistment
	{
        /// <summary>
        /// Forces the rollback.
        /// </summary>
        public abstract void ForceRollback();
        
        /// <summary>
        /// Forces the rollback.
        /// </summary>
        /// <param name="error">The error.</param>
        public abstract void ForceRollback(Exception error);
       
        /// <summary>
        /// Prepareds this instance.
        /// </summary>
        public abstract void Prepared();
	}
}
