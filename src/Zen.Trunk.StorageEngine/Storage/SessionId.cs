using System;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// Simple value type which represents a Session Identifier.
    /// </summary>
    [Serializable]
    public struct SessionId : IComparable, ICloneable
    {
        #region Public Fields
        /// <summary>
        /// The zero
        /// </summary>
        public static readonly SessionId Zero = new SessionId(0);
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionId"/> struct.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        public SessionId(uint sessionId)
        {
            Value = sessionId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets/sets the session identifier.
        /// </summary>
        public uint Value { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ConnectionId[{Value:X8}]";
        }

        /// <summary>
        /// Overridden. Tests obj for equality with this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var equal = false;
            if (obj is SessionId)
            {
                var rhs = (SessionId)obj;
                if (Value == rhs.Value)
                {
                    equal = true;
                }
            }
            return equal;
        }

        /// <summary>
        /// Overridden. Returns the hash code for this instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            // Use the virtual page hash code...
            return Value.GetHashCode();
        }

        /// <summary>
        /// Gets a value indicating the relative sort order when
        /// compared against another device page ID.
        /// </summary>
        /// <param name="obj">Object to be compared against.</param>
        /// <returns>
        /// <b>&lt;0</b> this object sorts lower than obj.
        /// <b>=0</b> this object sorts the same as obj.
        /// <b>&gt;0</b> this object sorts higher than obj.
        /// </returns>
        public int CompareTo(SessionId obj)
        {
            var order = Value.CompareTo(obj.Value);
            return order;
        }

        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator <(SessionId left, SessionId right)
        {
            return (left.Value < right.Value);
        }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator >(SessionId left, SessionId right)
        {
            return (left.Value > right.Value);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(SessionId left, SessionId right)
        {
            return (left.Value == right.Value);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(SessionId left, SessionId right)
        {
            return (left.Value != right.Value);
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            var order = -1;
            if (obj is SessionId)
            {
                order = CompareTo((SessionId)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public SessionId Clone()
        {
            return (SessionId)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return Clone();
        }
        #endregion
    }
}