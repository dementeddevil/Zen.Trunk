//
// NotInterestedMessage.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//



using System;
using System.Net;

namespace Zen.Trunk.Torrent.Client.Messages.Standard
{
    /// <summary>
    /// Represents a "NotInterested" message
    /// </summary>
    public class NotInterestedMessage : PeerMessage
    {
        private const int messageLength = 1;
        internal static readonly byte MessageId = 3;

        #region Constructors
        /// <summary>
        /// Creates a new NotInterestedMessage
        /// </summary>
        public NotInterestedMessage()
        {
        }
        #endregion


        #region Methods
        
        public override int Encode(byte[] buffer, int offset)
        {
			int written = offset;
			
			written += Write(buffer, offset, messageLength);
            written += Write(buffer, offset + 4, MessageId);

            CheckWritten(written - offset);
            return written - offset;
        }



        public override void Decode(byte[] buffer, int offset, int length)
        {
            // No decoding needed
        }

        /// <summary>
        /// Performs any necessary actions required to process the message
        /// </summary>
        /// <param name="id">The Peer who's message will be handled</param>
        internal override void Handle(PeerId id)
        {
            id.IsInterested = false;
        }


        /// <summary>
        /// Returns the length of the message in bytes
        /// </summary>
        public override int ByteLength
        {
            get { return (messageLength + 4); }
        }
        #endregion


        #region Overridden Methods
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "NotInterestedMessage";
        }

        public override bool Equals(object obj)
        {
            return (obj is NotInterestedMessage);
        }

        public override int GetHashCode()
        {
            return (this.ToString().GetHashCode());
        }
        #endregion
    }
}