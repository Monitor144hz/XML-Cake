﻿using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace XmlCake.Linq.Expressions;

public class XFilteredWrapExpression : IXExpression
{
	private XmlNodeType allowedNodeType { get; set; }
	public XFilteredWrapExpression(XmlNodeType nodeType, List<XStep> steps)
	{
		allowedNodeType = nodeType;
		matchSteps = steps;
	}
	public XFilteredWrapExpression(XmlNodeType nodeType,params XStep[] steps) : this(nodeType, steps.ToList()) { }

	public XMatch Match(List<XNode> nodes)
	{
		int p = 0;
		List<XNode> buffer = new List<XNode>();
		foreach (XNode node in nodes)
		{
			bool isMatch = matchSteps[p].IsMatch(node); 
			if (isMatch) { p++; }

			if (p > 0 && (node.NodeType == allowedNodeType || isMatch)) { buffer.Add(node); }

			if (p == matchSteps.Count) { return new XMatch(buffer); }

		}
		return new XMatch();

	}


	public XMatchCollection Matches(List<XNode> nodes)
	{
		int p = 0;
		List<XMatch> matchList = new List<XMatch>();
		List<XNode> buffer = new List<XNode>();
		foreach (XNode node in nodes)
		{
			bool isMatch = matchSteps[p].IsMatch(node);
			if (isMatch) { p++; }

			if (p > 0 && (node.NodeType == allowedNodeType || isMatch)) { buffer.Add(node); }

			if (p == matchSteps.Count)
			{
				p = 0;
				matchList.Add(new XMatch(buffer));
				buffer = new List<XNode>();
			}

		}
		return new XMatchCollection(matchList);
	}


	private List<XStep> matchSteps { get; set; } = new List<XStep>();
}