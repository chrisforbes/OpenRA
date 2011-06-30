#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class SymbolSet
	{
		int nextValue = 1;
		Cache<string, int> values;
		
		int Allocate() 
		{
			if (nextValue == 0)
				throw new InvalidOperationException("Out of bits");
			
			var ret = nextValue;
			nextValue <<= 1;
			return ret;
		}
		
		public SymbolSet()
		{
			values = Cache.New( (string _) => Allocate() );
		}
		
		public int GetValue( string[] syms )
		{
			return syms.Select( s => values[s] ).Aggregate( (a,b) => a | b );
		}
	}
}

