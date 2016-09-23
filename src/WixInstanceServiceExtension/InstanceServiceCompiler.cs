using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Tools.WindowsInstallerXml;
using Microsoft.Tools.WindowsInstallerXml.Extensions;

namespace Zen.WindowsInstallerXml.Extensions
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class InstanceServiceCompiler : CompilerExtension
	{
		#region Private Fields
		// Group creation attribute definitions (from sca.h)
		internal const int GroupFailIfExists = 0x00000010;
		internal const int GroupUpdateIfExists = 0x00000020;
		internal const int GroupDontRemoveOnUninstall = 0x00000100;
		internal const int GroupDontCreateGroup = 0x00000200;

		// User creation attribute definitions
		internal const int UserDontExpirePassword = 0x00000001;
		internal const int UserPasswordCantChange = 0x00000002;
		internal const int UserChangePasswordOnLogin = 0x00000004;
		internal const int UserDisableAccount = 0x00000008;
		internal const int UserFailIfExists = 0x00000010;
		internal const int UserUpdateIfExists = 0x00000020;
		internal const int UserAllowLogonAsService = 0x00000040;
		internal const int UserIgnoreServiceAccounts = 0x00000080;
		internal const int UserDontRemoveOnUninstall = 0x00000100;
		internal const int UserDontCreateUser = 0x00000200;

		// Url Reservation creation attribute definitions
		internal const int UrlReservationFailIfExists = 0x00000001;
		internal const int UrlReservationUpdateIfExists = 0x00000002;
		internal const int UrlReservationDontRemoveOnUninstall = 0x00000004;

		// Url Reservation ACL attribute definitions
		internal const int UrlReservationAclCanRegister = 0x00000001;
		internal const int UrlReservationAclCanDelegate = 0x00000002;

		private static readonly Regex FindProperty = new Regex(
			@"(\[.*?\])",
			RegexOptions.ExplicitCapture | RegexOptions.Compiled);
		#endregion

		#region Public Constructors
		/// <summary>
		/// Initialises an instance of <see cref="T:SUICompiler" />.
		/// </summary>
		public InstanceServiceCompiler()
		{
			Schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Zen.WindowsInstallerXml.Extensions.Xsd.InstanceService.xsd");
			System.Diagnostics.Debug.Assert(Schema != null);
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the schema.
		/// </summary>
		/// <value>The schema.</value>
		public override XmlSchema Schema { get; }
	    #endregion

		#region Public Methods
		/// <summary>
		/// Parses the element.
		/// </summary>
		/// <param name="sourceLineNumbers">The source line numbers.</param>
		/// <param name="parentElement">The parent element.</param>
		/// <param name="element">The element.</param>
		/// <param name="contextValues">The context values.</param>
		public override void ParseElement(
			SourceLineNumberCollection sourceLineNumbers,
			XmlElement parentElement,
			XmlElement element,
			params string[] contextValues)
		{
			switch (parentElement.LocalName)
			{
				case "Fragment":
				case "Module":
				case "Product":
					var uiRef = contextValues[0];

					switch (element.LocalName)
					{
						case "GroupEx":
							ParseGroupElement(element, null);
							break;
						case "UserEx":
							ParseUserElement(element, null);
							break;
						case "UIRef":
							ParseUIRefElement(element, uiRef);
							break;
						default:
							Core.UnexpectedElement(parentElement, element);
							break;
					}
					break;
				case "Component":
					var componentId = contextValues[0];
					var directoryId = contextValues[1];

					switch (element.LocalName)
					{
						case "GroupEx":
							ParseGroupElement(element, componentId);
							break;
						case "UserEx":
							ParseUserElement(element, componentId);
							break;
						case "UrlReservation":
							ParseUrlReservationElement(element, componentId);
							break;
						default:
							Core.UnexpectedElement(parentElement, element);
							break;
					}
					break;
				default:
					Core.UnexpectedElement(parentElement, element);
					break;
			}
		}
		#endregion

		#region Protected Methods
		#endregion

		#region Private Methods
		/// <summary>
		/// Parses a group element.
		/// </summary>
		/// <param name="node">Node to be parsed.</param>
		/// <param name="componentId">Component Id of the parent component of this element.</param>
		private void ParseGroupElement(XmlNode node, string componentId)
		{
			var sourceLineNumbers =
				Preprocessor.GetSourceLineNumbers(node);
			string id = null;
			string domain = null;
			string name = null;
			string description = null;
			var attributes = 0;

			// get specifics of our node
			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length ||
					attrib.NamespaceURI == Schema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							id = Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
							break;
						case "Name":
							name = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "Domain":
							domain = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "Description":
							description = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "CreateGroup":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= GroupDontCreateGroup;
							}
							break;
						case "FailIfExists":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= GroupFailIfExists;
							}
							break;
						case "RemoveOnUninstall":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= GroupDontRemoveOnUninstall;
							}
							break;
						case "UpdateIfExists":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= GroupUpdateIfExists;
							}
							break;
						default:
							Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
				}
			}

			if (null == id)
			{
				Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					if (child.NamespaceURI == Schema.TargetNamespace)
					{
						Core.UnexpectedElement(node, child);
					}
					else
					{
						Core.UnsupportedExtensionElement(node, child);
					}
				}
			}

			if (!Core.EncounteredError)
			{
				var row = Core.CreateRow(sourceLineNumbers, "GroupEx");
				row[0] = id;
				row[1] = componentId;
				row[2] = name;
				row[3] = domain;
				row[4] = description;
				row[5] = attributes;

				row = Core.CreateRow(sourceLineNumbers, "Group");
				row[0] = id;
				row[1] = componentId;
				row[2] = name;
				row[3] = domain;

				Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "SuiSchedGroupsInstall");
				Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "SuiSchedGroupsUninstall");
			}
		}

		/// <summary>
		/// Parses a user element.
		/// </summary>
		/// <param name="node">Node to be parsed.</param>
		/// <param name="componentId">Component Id of the parent component of this element.</param>
		private void ParseUserElement(XmlNode node, string componentId)
		{
			var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
			string id = null;
			string domain = null;
			string name = null;
			string password = null;
			var attributes = 0;

			// get specifics of our node
			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == Schema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							id = Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
							break;
						case "Name":
							name = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "Domain":
							domain = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "Password":
							password = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "CanNotChangePassword":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserPasswordCantChange;
							}
							break;
						case "CreateUser":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserDontCreateUser;
							}
							break;
						case "Disabled":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserDisableAccount;
							}
							break;
						case "PasswordExpired":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserChangePasswordOnLogin;
							}
							break;
						case "PasswordNeverExpires":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserDontExpirePassword;
							}
							break;
						case "LogonAsService":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserAllowLogonAsService;
							}
							break;
						case "IgnoreServiceAccounts":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserIgnoreServiceAccounts;
							}
							break;
						case "FailIfExists":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserFailIfExists;
							}
							break;
						case "RemoveOnUninstall":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserDontRemoveOnUninstall;
							}
							break;
						case "UpdateIfExists":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UserUpdateIfExists;
							}
							break;
						default:
							Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
				}
			}

			if (null == id)
			{
				Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == name)
			{
				Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					if (child.NamespaceURI == Schema.TargetNamespace)
					{
						var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

						switch (child.LocalName)
						{
							case "GroupRef":
								if (null == componentId)
								{
									Core.OnMessage(UtilErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
								}

								ParseGroupRefElement(child, id);
								break;
							default:
								Core.UnexpectedElement(node, child);
								break;
						}
					}
					else
					{
						Core.UnsupportedExtensionElement(node, child);
					}
				}
			}

			if (null != componentId)
			{
				Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "SuiSchedUsersInstall");
				Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "SuiSchedUsersUninstall");
			}

			if (!Core.EncounteredError)
			{
				var row = Core.CreateRow(sourceLineNumbers, "UserEx");
				row[0] = id;
				row[1] = componentId;
				row[2] = name;
				row[3] = domain;
				row[4] = password;
				row[5] = attributes;

				row = Core.CreateRow(sourceLineNumbers, "User");
				row[0] = id;
				row[1] = componentId;
				row[2] = name;
				row[3] = domain;
				row[4] = password;
				row[5] = 0;
			}
		}

		/// <summary>
		/// Parses a GroupRef element
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="userId">Required user id to be joined to the group.</param>
		private void ParseGroupRefElement(XmlNode node, string userId)
		{
			var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
			string groupId = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == Schema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							groupId = Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
							Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Group", groupId);
							break;
						default:
							Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
				}
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					if (child.NamespaceURI == Schema.TargetNamespace)
					{
						Core.UnexpectedElement(node, child);
					}
					else
					{
						Core.UnsupportedExtensionElement(node, child);
					}
				}
			}

			if (!Core.EncounteredError)
			{
				var row = Core.CreateRow(sourceLineNumbers, "UserGroup");
				row[0] = userId;
				row[1] = groupId;
			}
		}

		/// <summary>
		/// Parses the UI ref element.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="uiRef">The UI ref.</param>
		private void ParseUIRefElement(XmlNode node, string uiRef)
		{
			var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
			string id = null;
			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							id = Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
							break;

						default:
							Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
				}
			}

			// Id is required...
			if (null == id)
			{
				Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			// Okay determine references
			if (!Core.EncounteredError)
			{
				switch (id)
				{
					case "WixUI_InstanceServiceDomainInstall":
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "UpdateFreeServiceInstance");
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ValidateInstanceName");
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ValidateServiceCredentials");
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ValidateDomainServiceCredentials");
						break;
					case "WixUI_InstanceServiceInstall":
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "UpdateFreeServiceInstance");
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ValidateInstanceName");
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ValidateServiceCredentials");
						break;
					case "WixUI_ServiceDomainInstall":
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ValidateServiceCredentials");
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ValidateDomainServiceCredentials");
						break;
					case "WixUI_ServiceInstall":
						Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ValidateServiceCredentials");
						break;
				}
			}
		}

		/// <summary>
		/// Parses a UrlReservation element.
		/// </summary>
		/// <param name="node">Node to be parsed.</param>
		/// <param name="componentId">Component Id of the parent component of this element.</param>
		private void ParseUrlReservationElement(
			XmlNode node, string componentId)
		{
			var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
			string id = null;
			string url = null;
			var attributes = 0;

			// get specifics of our node
			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length ||
					attrib.NamespaceURI == Schema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							id = Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
							break;
						case "Url":
							url = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "FailIfExists":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UrlReservationFailIfExists;
							}
							break;
						case "RemoveOnUninstall":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.No == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UrlReservationDontRemoveOnUninstall;
							}
							break;
						case "UpdateIfExists":
							if (null == componentId)
							{
								Core.OnMessage(UtilErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
							}

							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UrlReservationUpdateIfExists;
							}
							break;
						default:
							Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
				}
			}

			if (null == id)
			{
				Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == url)
			{
				Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Url"));
			}

			// find unexpected child elements
			var aclCount = 0;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					if (child.NamespaceURI == Schema.TargetNamespace)
					{
						var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

						switch (child.LocalName)
						{
							case "UrlReservationAcl":
								if (null == componentId)
								{
									Core.OnMessage(UtilErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
								}

								ParseUrlReservationAclElement(child, id);
								++aclCount;
								break;
							default:
								Core.UnexpectedElement(node, child);
								break;
						}
					}
					else
					{
						Core.UnsupportedExtensionElement(node, child);
					}
				}
			}

			if (aclCount == 0)
			{
				Core.OnMessage(WixErrors.ExpectedElement(
					sourceLineNumbers, node.Name, "UrlReservationAcl"));
			}

			if (null != componentId)
			{
				Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "SuiSchedUrlReservationsInstall");
				Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "SuiSchedUrlReservationsUninstall");
			}

			if (!Core.EncounteredError)
			{
				var row = Core.CreateRow(sourceLineNumbers, "UrlReservation");
				row[0] = id;
				row[1] = componentId;
				row[2] = url;
				row[3] = attributes;
			}
		}

		/// <summary>
		/// Parses the URL reservation acl element.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="reservationId">The reservation id.</param>
		private void ParseUrlReservationAclElement(
			XmlNode node, string reservationId)
		{
			var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
			string id = null;
			string name = null;
			string domain = null;
			var attributes = 0;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == Schema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							id = Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
							break;
						case "Name":
							name = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "Domain":
							domain = Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "CanRegister":
							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UrlReservationAclCanRegister;
							}
							break;
						case "CanDelegate":
							if (YesNoType.Yes == Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
							{
								attributes |= UrlReservationAclCanDelegate;
							}
							break;
						default:
							Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
				}
			}

			if (null == id)
			{
				Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == name)
			{
				Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					if (child.NamespaceURI == Schema.TargetNamespace)
					{
						Core.UnexpectedElement(node, child);
					}
					else
					{
						Core.UnsupportedExtensionElement(node, child);
					}
				}
			}

			if (!Core.EncounteredError)
			{
				var row = Core.CreateRow(sourceLineNumbers, "UrlReservationAcl");
				row[0] = id;
				row[1] = reservationId;
				row[2] = name;
				row[3] = domain;
				row[4] = attributes;
			}
		}
		#endregion
	}
}
