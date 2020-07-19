﻿using System;

namespace Morestachio.Framework
{
	/// <summary>
	///		Defines the output that can count on written bytes into a stream
	/// </summary>
	public interface IByteCounterStream : IDisposable
	{
		/// <summary>
		/// Gets or sets the bytes written.
		/// </summary>
		/// <value>
		/// The bytes written.
		/// </value>
		long BytesWritten { get; }

		/// <summary>
		/// Gets or sets a value indicating whether [reached limit].
		/// </summary>
		/// <value>
		///   <c>true</c> if [reached limit]; otherwise, <c>false</c>.
		/// </value>
		bool ReachedLimit { get; }


		/// <summary>
		///		Writes the Content into the underlying Stream when the limit is not exceeded
		/// </summary>
		/// <param name="content"></param>
		void Write(string content);
	}
}