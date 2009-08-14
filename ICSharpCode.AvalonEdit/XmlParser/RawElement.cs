﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.XmlParser
{
	/// <summary>
	/// Logical grouping of other nodes together.
	/// </summary>
	public class RawElement: RawContainer
	{
		/// <summary> No tags are missing anywhere within this element (recursive) </summary>
		public bool IsProperlyNested { get; set; }
		/// <returns> True in wellformed XML </returns>
		public bool HasStartOrEmptyTag { get; set; }
		/// <returns> True in wellformed XML </returns>
		public bool HasEndTag { get; set; }
		
		/// <summary>  StartTag of an element.  </summary>
		public RawTag StartTag {
			get {
				return (RawTag)this.Children[0];
			}
		}
		
		internal override void DebugCheckConsistency(bool allowNullParent)
		{
			DebugAssert(Children.Count > 0, "No children");
			base.DebugCheckConsistency(allowNullParent);
		}
		
		#region Helpper methods
		
		AttributeCollection attributes;
		
		/// <summary> Gets attributes of the element </summary>
		public AttributeCollection Attributes {
			get {
				if (attributes == null) {
					attributes = new AttributeCollection(this.StartTag.Children);
				}
				return attributes;
			}
		}
		
		ObservableCollection<RawObject> attributesAndElements;
		
		// TODO: Identity
		/// <summary> Gets both attributes and elements </summary>
		public ObservableCollection<RawObject> AttributesAndElements {
			get {
				if (attributesAndElements == null) {
					attributesAndElements = new MergedCollection<RawObject, ObservableCollection<RawObject>> (
						// New wrapper with RawObject types
						new FilteredCollection<RawObject, ChildrenCollection<RawObject>>(this.StartTag.Children, x => x is RawAttribute),
						new FilteredCollection<RawObject, ChildrenCollection<RawObject>>(this.Children, x => x is RawElement)
					);
				}
				return attributesAndElements;
			}
		}
		
		/// <summary> Name with namespace prefix - exactly as in source </summary>
		public string Name {
			get {
				return this.StartTag.Name;
			}
		}
		
		/// <summary> The part of name before ":" </summary>
		/// <returns> Empty string if not found </returns>
		public string Prefix {
			get {
				return GetNamespacePrefix(this.StartTag.Name);
			}
		}
		
		/// <summary> The part of name after ":" </summary>
		/// <returns> Empty string if not found </returns>
		public string LocalName {
			get {
				return GetLocalName(this.StartTag.Name);
			}
		}
		
		/// <summary> Resolved namespace of the name </summary>
		/// <returns> Empty string if prefix is not found </returns>
		public string Namespace {
			get {
				string prefix = this.Prefix;
				if (string.IsNullOrEmpty(prefix)) {
					return FindDefaultNamesapce();
				} else {
					return ReslovePrefix(prefix);
				}
			}
		}
		
		/// <summary> Find the defualt namesapce for this context </summary>
		public string FindDefaultNamesapce()
		{
			RawElement current = this;
			while(current != null) {
				string namesapce = current.GetAttributeValue(NoNamespace, "xmlns");
				if (namesapce != null) return namesapce;
				current = current.Parent as RawElement;
			}
			return string.Empty; // No namesapce
		}
		
		/// <summary>
		/// Recursively resolve given prefix in this context.  Prefix must have some value.
		/// </summary>
		/// <returns> Empty string if prefix is not found </returns>
		public string ReslovePrefix(string prefix)
		{
			if (string.IsNullOrEmpty(prefix)) throw new ArgumentException("No prefix given", "prefix");
			
			// Implicit namesapces
			if (prefix == "xml") return XmlNamespace;
			if (prefix == "xmlns") return XmlnsNamespace;
			
			RawElement current = this;
			while(current != null) {
				string namesapce = current.GetAttributeValue(XmlnsNamespace, prefix);
				if (namesapce != null) return namesapce;
				current = current.Parent as RawElement;
			}
			return NoNamespace; // Can not find prefix
		}
		
		/// <summary>
		/// Get unquoted value of attribute.
		/// It looks in the no namespace (empty string).
		/// </summary>
		/// <returns>Null if not found</returns>
		public string GetAttributeValue(string localName)
		{
			return GetAttributeValue(NoNamespace, localName);
		}
		
		/// <summary>
		/// Get unquoted value of attribute
		/// </summary>
		/// <param name="namespace">Namespace.  Can be no namepace (empty string), which is the default for attributes.</param>
		/// <param name="localName">Local name - text after ":"</param>
		/// <returns>Null if not found</returns>
		public string GetAttributeValue(string @namespace, string localName)
		{
			@namespace = @namespace ?? string.Empty;
			foreach(RawAttribute attr in this.Attributes.GetByLocalName(localName)) {
				DebugAssert(attr.LocalName == localName, "Bad hashtable");
				if (attr.Namespace == @namespace) {
					return attr.Value;
				}
			}
			return null;
		}
		
		#endregion
		
		/// <inheritdoc/>
		public override void AcceptVisitor(IXmlVisitor visitor)
		{
			visitor.VisitElement(this);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format("[{0} '{1}{2}{3}' Attr:{4} Chld:{5} Nest:{6}]", base.ToString(), this.StartTag.OpeningBracket, this.StartTag.Name, this.StartTag.ClosingBracket, this.StartTag.Children.Count, this.Children.Count, this.IsProperlyNested ? "Ok" : "Bad");
		}
	}
}