//
// CancelMessage.cs
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
using Zen.Trunk.Torrent.Client.Messages;

namespace Zen.Trunk.Torrent.Client.Messages.Standard
{
    /// <summary>
    /// 
    /// </summary>
    public class CancelMessage : PeerMessage
    {
        private const int messageLength = 13;
        internal static readonly byte MessageId = 8;


        #region Member Variables
        /// <summary>
        /// The index of the piece
        /// </summary>
        public int PieceIndex
        {
            get { return this.pieceIndex; }
        }
        private int pieceIndex;


        /// <summary>
        /// The offset in bytes of the block of data
        /// </summary>
        public int StartOffset
        {
            get { return this.startOffset; }
        }
        private int startOffset;


        /// <summary>
        /// The length in bytes of the block of data
        /// </summary>
        public int RequestLength
        {
            get { return this.requestLength; }
        }
        private int requestLength;
        #endregion


        #region Constructors
        /// <summary>
        /// Creates a new CancelMessage
        /// </summary>
        public CancelMessage()
        {
        }


        /// <summary>
        /// Creates a new CancelMessage
        /// </summary>
        /// <param name="pieceIndex">The index of the piece to cancel</param>
        /// <param name="startOffset">The offset in bytes of the block of data to cancel</param>
        /// <param name="requestLength">The length in bytes of the block of data to cancel</param>
        public CancelMessage(int pieceIndex, int startOffset, int requestLength)
        {
            this.pieceIndex = pieceIndex;
            this.startOffset = startOffset;
            this.requestLength = requestLength;
        }
        #endregion


        #region Methods
        public override int Encode(byte[] buffer, int offset)
        {
			int written = offset;
			
			written += Write(buffer, offset, messageLength);
            written += Write(buffer, offset + 4, MessageId);
            written += Write(buffer, offset + 5, pieceIndex);
            written += Write(buffer, offset + 9, startOffset);
            written += Write(buffer, offset + 13, requestLength);

            CheckWritten(written - offset);
            return written - offset;
        }

        public override void Decode(byte[] buffer, int offset, int length)
        {
            pieceIndex = ReadInt(buffer, offset);
            startOffset = ReadInt(buffer, offset + 4);
            requestLength = ReadInt(buffer, offset + 8);
        }

        /// <summary>
        /// Performs any necessary actions required to process the message
        /// </summary>
        /// <param name="id">The Peer who's message will be handled</param>
        internal override void Handle(PeerId id)
        {
            PeerMessage msg;
            for (int i = 0; i < id.QueueLength; i++)
            {
                msg = id.Dequeue();
                if (!(msg is PieceMessage))
                {
                    id.Enqueue(msg);
                    continue;
                }

                PieceMessage piece = msg as PieceMessage;
                if (!(piece.PieceIndex == this.pieceIndex && piece.StartOffset == this.startOffset && piece.RequestLength == this.requestLength))
                {
                    id.Enqueue(msg);
                }
                else
                {
                    id.IsRequestingPiecesCount--;
                }
            }
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
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("CancelMessage ");
            sb.Append(" Index ");
            sb.Append(this.pieceIndex);
            sb.Append(" Offset ");
            sb.Append(this.startOffset);
            sb.Append(" Length ");
            sb.Append(this.requestLength);
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            CancelMessage msg = obj as CancelMessage;

            if (msg == null)
                return false;

            return (this.pieceIndex == msg.pieceIndex
                    && this.startOffset == msg.startOffset
                    && this.requestLength == msg.requestLength);
        }

        public override int GetHashCode()
        {
            return (this.pieceIndex.GetHashCode()
                ^ this.requestLength.GetHashCode()
                ^ this.startOffset.GetHashCode());
        }
        #endregion
    }
}