using System;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace System.Data
{
	internal sealed class EDesignRes
	{
		internal const string EntityStoreGeneratorSchemaNotLoaded = "EntityStoreGeneratorSchemaNotLoaded";

		internal const string EntityModelGeneratorSchemaNotLoaded = "EntityModelGeneratorSchemaNotLoaded";

		internal const string StonglyTypedAccessToNullValue = "StonglyTypedAccessToNullValue";

		internal const string NoPrimaryKeyDefined = "NoPrimaryKeyDefined";

		internal const string InvalidTypeForPrimaryKey = "InvalidTypeForPrimaryKey";

		internal const string CannotCreateEntityWithNoPrimaryKeyDefined = "CannotCreateEntityWithNoPrimaryKeyDefined";

		internal const string TableReferencedByAssociationWasNotFound = "TableReferencedByAssociationWasNotFound";

		internal const string TableReferencedByTvfWasNotFound = "TableReferencedByTvfWasNotFound";

		internal const string UnsupportedDataType = "UnsupportedDataType";

		internal const string UnsupportedDataTypeUnknownType = "UnsupportedDataTypeUnknownType";

		internal const string UnsupportedFunctionReturnDataType = "UnsupportedFunctionReturnDataType";

		internal const string UnsupportedFunctionParameterDataType = "UnsupportedFunctionParameterDataType";

		internal const string UnsupportedDataTypeForTarget = "UnsupportedDataTypeForTarget";

		internal const string UnsupportedFunctionReturnDataTypeForTarget = "UnsupportedFunctionReturnDataTypeForTarget";

		internal const string UnsupportedFunctionParameterDataTypeForTarget = "UnsupportedFunctionParameterDataTypeForTarget";

		internal const string UnsupportedDbRelationship = "UnsupportedDbRelationship";

		internal const string ParameterDirectionNotValid = "ParameterDirectionNotValid";

		internal const string InvalidStringArgument = "InvalidStringArgument";

		internal const string Serialization_UnknownGlobalItem = "Serialization_UnknownGlobalItem";

		internal const string ReservedNamespace = "ReservedNamespace";

		internal const string ColumnFacetValueOutOfRange = "ColumnFacetValueOutOfRange";

		internal const string AssociationMissingKeyColumn = "AssociationMissingKeyColumn";

		internal const string SingleStoreEntityContainerExpected = "SingleStoreEntityContainerExpected";

		internal const string InvalidNonStoreEntityContainer = "InvalidNonStoreEntityContainer";

		internal const string ExcludedColumnWasAKeyColumnEntityIsInvalid = "ExcludedColumnWasAKeyColumnEntityIsInvalid";

		internal const string ExcludedColumnWasAKeyColumnEntityIsReadOnly = "ExcludedColumnWasAKeyColumnEntityIsReadOnly";

		internal const string ModelGeneration_UnGeneratableType = "ModelGeneration_UnGeneratableType";

		internal const string DuplicateEntityContainerName = "DuplicateEntityContainerName";

		internal const string ProviderFactoryReturnedNullFactory = "ProviderFactoryReturnedNullFactory";

		internal const string ProviderSchemaErrors = "ProviderSchemaErrors";

		internal const string InvalidNamespaceNameArgument = "InvalidNamespaceNameArgument";

		internal const string InvalidEntityContainerNameArgument = "InvalidEntityContainerNameArgument";

		internal const string EntityClient_InvalidStoreProvider = "EntityClient_InvalidStoreProvider";

		internal const string DbProviderServicesInformationLocationPath = "DbProviderServicesInformationLocationPath";

		internal const string UnsupportedForeignKeyPattern = "UnsupportedForeignKeyPattern";

		internal const string UnsupportedQueryViewInEntityContainerMapping = "UnsupportedQueryViewInEntityContainerMapping";

		internal const string SharedForeignKey = "SharedForeignKey";

		internal const string UnmappedFunctionImport = "UnmappedFunctionImport";

		internal const string CannotChangePropertyReturnType = "CannotChangePropertyReturnType";

		internal const string CannotChangePropertyReturnTypeToNull = "CannotChangePropertyReturnTypeToNull";

		internal const string CodeGenSourceFilePathIsNotAFile = "CodeGenSourceFilePathIsNotAFile";

		internal const string InvalidAttributeSuppliedForType = "InvalidAttributeSuppliedForType";

		internal const string InvalidMemberSuppliedForType = "InvalidMemberSuppliedForType";

		internal const string InvalidInterfaceSuppliedForType = "InvalidInterfaceSuppliedForType";

		internal const string InvalidAttributeSuppliedForProperty = "InvalidAttributeSuppliedForProperty";

		internal const string InvalidGetStatementSuppliedForProperty = "InvalidGetStatementSuppliedForProperty";

		internal const string InvalidSetStatementSuppliedForProperty = "InvalidSetStatementSuppliedForProperty";

		internal const string PropertyExistsWithDifferentCase = "PropertyExistsWithDifferentCase";

		internal const string EntitySetExistsWithDifferentCase = "EntitySetExistsWithDifferentCase";

		internal const string ItemExistsWithDifferentCase = "ItemExistsWithDifferentCase";

		internal const string NullAdditionalSchema = "NullAdditionalSchema";

		internal const string DuplicateClassName = "DuplicateClassName";

		internal const string TargetEntityFrameworkVersionToNewForEntityClassGenerator = "TargetEntityFrameworkVersionToNewForEntityClassGenerator";

		internal const string MissingPropertyDocumentation = "MissingPropertyDocumentation";

		internal const string MissingComplexTypeDocumentation = "MissingComplexTypeDocumentation";

		internal const string MissingDocumentation = "MissingDocumentation";

		internal const string MissingDocumentationNoName = "MissingDocumentationNoName";

		internal const string NamespaceComments = "NamespaceComments";

		internal const string FactoryMethodSummaryComment = "FactoryMethodSummaryComment";

		internal const string FactoryParamCommentGeneral = "FactoryParamCommentGeneral";

		internal const string CtorSummaryComment = "CtorSummaryComment";

		internal const string EmptyCtorSummaryComment = "EmptyCtorSummaryComment";

		internal const string GeneratedNavigationPropertyNameConflict = "GeneratedNavigationPropertyNameConflict";

		internal const string GeneratedPropertyAccessibilityConflict = "GeneratedPropertyAccessibilityConflict";

		internal const string EntityTypeAndSetAccessibilityConflict = "EntityTypeAndSetAccessibilityConflict";

		internal const string GeneratedFactoryMethodNameConflict = "GeneratedFactoryMethodNameConflict";

		internal const string MetadataItemErrorsFoundDuringGeneration = "MetadataItemErrorsFoundDuringGeneration";

		internal const string UnableToGenerateForeignKeyPropertiesForV1 = "UnableToGenerateForeignKeyPropertiesForV1";

		internal const string UnableToGenerateFunctionImportParameterName = "UnableToGenerateFunctionImportParameterName";

		internal const string TypeComments = "TypeComments";

		internal const string GetViewAtMethodComments = "GetViewAtMethodComments";

		internal const string ConstructorComments = "ConstructorComments";

		internal const string IndividualViewComments = "IndividualViewComments";

		internal const string TargetVersionSchemaVersionMismatch = "TargetVersionSchemaVersionMismatch";

		internal const string DuplicateEntryInUserDictionary = "DuplicateEntryInUserDictionary";

		internal const string UnsupportedLocaleForPluralizationServices = "UnsupportedLocaleForPluralizationServices";

		internal const string Template_DuplicateTopLevelType = "Template_DuplicateTopLevelType";

		internal const string Template_ConflictingGeneratedNavPropName = "Template_ConflictingGeneratedNavPropName";

		internal const string Template_FactoryMethodNameConflict = "Template_FactoryMethodNameConflict";

		internal const string Template_CaseInsensitiveTypeConflict = "Template_CaseInsensitiveTypeConflict";

		internal const string Template_CaseInsensitiveEntitySetConflict = "Template_CaseInsensitiveEntitySetConflict";

		internal const string Template_CaseInsensitiveMemberConflict = "Template_CaseInsensitiveMemberConflict";

		internal const string Template_GenCommentAddToMethodCs = "Template_GenCommentAddToMethodCs";

		internal const string Template_GenCommentAddToMethodVb = "Template_GenCommentAddToMethodVb";

		internal const string Template_CommentNoDocumentation = "Template_CommentNoDocumentation";

		internal const string Template_CommentFactoryMethodParam = "Template_CommentFactoryMethodParam";

		internal const string Template_GeneratedCodeCommentLine1 = "Template_GeneratedCodeCommentLine1";

		internal const string Template_GeneratedCodeCommentLine2 = "Template_GeneratedCodeCommentLine2";

		internal const string Template_GeneratedCodeCommentLine3 = "Template_GeneratedCodeCommentLine3";

		internal const string Template_ContextDefaultCtorComment = "Template_ContextDefaultCtorComment";

		internal const string Template_ContextCommonCtorComment = "Template_ContextCommonCtorComment";

		internal const string Template_FactoryMethodComment = "Template_FactoryMethodComment";

		internal const string Template_RegionRelationships = "Template_RegionRelationships";

		internal const string Template_RegionContexts = "Template_RegionContexts";

		internal const string Template_RegionObjectSetProperties = "Template_RegionObjectSetProperties";

		internal const string Template_RegionAddToMethods = "Template_RegionAddToMethods";

		internal const string Template_RegionFunctionImports = "Template_RegionFunctionImports";

		internal const string Template_RegionEntities = "Template_RegionEntities";

		internal const string Template_RegionNavigationProperties = "Template_RegionNavigationProperties";

		internal const string Template_RegionComplexTypes = "Template_RegionComplexTypes";

		internal const string Template_RegionFactoryMethod = "Template_RegionFactoryMethod";

		internal const string Template_RegionPrimitiveProperties = "Template_RegionPrimitiveProperties";

		internal const string Template_RegionSimpleProperties = "Template_RegionSimpleProperties";

		internal const string Template_RegionComplexProperties = "Template_RegionComplexProperties";

		internal const string Template_RegionEnumTypes = "Template_RegionEnumTypes";

		internal const string Template_RegionConstructors = "Template_RegionConstructors";

		internal const string Template_RegionPartialMethods = "Template_RegionPartialMethods";

		internal const string Template_ReplaceVsItemTemplateToken = "Template_ReplaceVsItemTemplateToken";

		internal const string Template_CurrentlyRunningTemplate = "Template_CurrentlyRunningTemplate";

		internal const string Template_UnsupportedSchema = "Template_UnsupportedSchema";

		internal const string EdmSchemaNotValid = "EdmSchemaNotValid";

		internal const string EdmSchemaFileNotFound = "EdmSchemaFileNotFound";

		internal const string EntityCodeGenTargetTooLow = "EntityCodeGenTargetTooLow";

		internal const string DefaultTargetVersionTooLow = "DefaultTargetVersionTooLow";

		internal const string EntityClient_DoesNotImplementIServiceProvider = "EntityClient_DoesNotImplementIServiceProvider";

		internal const string EntityClient_ReturnedNullOnProviderMethod = "EntityClient_ReturnedNullOnProviderMethod";

		private static EDesignRes loader;

		private ResourceManager resources;

		private static CultureInfo Culture
		{
			get
			{
				return null;
			}
		}

		public static ResourceManager Resources
		{
			get
			{
				return EDesignRes.GetLoader().resources;
			}
		}

		internal EDesignRes()
		{
			this.resources = new ResourceManager("System.Data.Entity.Design", base.GetType().Assembly);
		}

		private static EDesignRes GetLoader()
		{
			if (EDesignRes.loader == null)
			{
				EDesignRes value = new EDesignRes();
				Interlocked.CompareExchange<EDesignRes>(ref EDesignRes.loader, value, null);
			}
			return EDesignRes.loader;
		}

		public static string GetString(string name, params object[] args)
		{
			EDesignRes eDesignRes = EDesignRes.GetLoader();
			if (eDesignRes == null)
			{
				return null;
			}
			string @string = eDesignRes.resources.GetString(name, EDesignRes.Culture);
			if (args != null && args.Length > 0)
			{
				for (int i = 0; i < args.Length; i++)
				{
					string text = args[i] as string;
					if (text != null && text.Length > 1024)
					{
						args[i] = text.Substring(0, 1021) + "...";
					}
				}
				return string.Format(CultureInfo.CurrentCulture, @string, args);
			}
			return @string;
		}

		public static string GetString(string name)
		{
			EDesignRes eDesignRes = EDesignRes.GetLoader();
			if (eDesignRes == null)
			{
				return null;
			}
			return eDesignRes.resources.GetString(name, EDesignRes.Culture);
		}

		public static string GetString(string name, out bool usedFallback)
		{
			usedFallback = false;
			return EDesignRes.GetString(name);
		}

		public static object GetObject(string name)
		{
			EDesignRes eDesignRes = EDesignRes.GetLoader();
			if (eDesignRes == null)
			{
				return null;
			}
			return eDesignRes.resources.GetObject(name, EDesignRes.Culture);
		}
	}
}
