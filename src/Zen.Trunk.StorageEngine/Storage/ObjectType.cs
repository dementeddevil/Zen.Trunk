﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage
{
    /// <summary>
    /// Simple value type which represents an Object Type.
    /// </summary>
    [Serializable]
    public struct ObjectType : IComparable, ICloneable
    {
        #region Public Fields
        public static readonly ObjectType Unknown = new ObjectType(0);
        public static readonly ObjectType Sample = new ObjectType(1);
        public static readonly ObjectType Table = new ObjectType(2);
        public static readonly ObjectType View = new ObjectType(3);
        #endregion

        #region Private Fields
        #endregion

        #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectType"/> struct.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        [CLSCompliant(false)]
        public ObjectType(byte objectType)
        {
            Value = objectType;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the object type value.
        /// </summary>
        [CLSCompliant(false)]
        public byte Value { get; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Overridden. Gets a string representation of the type.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"ObjectType{Value:X2}";
        }

        /// <summary>
        /// Overridden. Tests obj for equality with this instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var equal = false;
            if (obj is ObjectType)
            {
                var rhs = (ObjectType)obj;
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
        public int CompareTo(ObjectType obj)
        {
            var order = Value.CompareTo(obj.Value);
            return order;
        }

        public static bool operator <(ObjectType left, ObjectType right)
        {
            return (left.Value < right.Value);
        }

        public static bool operator >(ObjectType left, ObjectType right)
        {
            return (left.Value > right.Value);
        }

        public static bool operator ==(ObjectType left, ObjectType right)
        {
            return (left.Value == right.Value);
        }
        public static bool operator !=(ObjectType left, ObjectType right)
        {
            return (left.Value != right.Value);
        }
        #endregion

        #region IComparable Members
        int IComparable.CompareTo(object obj)
        {
            var order = -1;
            if (obj is ObjectType)
            {
                order = ((ObjectType)this).CompareTo((ObjectType)obj);
            }
            return order;
        }
        #endregion

        #region ICloneable Members
        public ObjectType Clone()
        {
            return (ObjectType)MemberwiseClone();
        }
        object ICloneable.Clone()
        {
            return ((ObjectType)this).Clone();
        }
        #endregion
    }
}