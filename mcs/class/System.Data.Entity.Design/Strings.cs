using System;

namespace System.Data.Entity.Design
{
	internal static class Strings
	{
		internal static string EntityStoreGeneratorSchemaNotLoaded
		{
			get
			{
				return EDesignRes.GetString("EntityStoreGeneratorSchemaNotLoaded");
			}
		}

		internal static string EntityModelGeneratorSchemaNotLoaded
		{
			get
			{
				return EDesignRes.GetString("EntityModelGeneratorSchemaNotLoaded");
			}
		}

		internal static string SingleStoreEntityContainerExpected
		{
			get
			{
				return EDesignRes.GetString("SingleStoreEntityContainerExpected");
			}
		}

		internal static string ProviderSchemaErrors
		{
			get
			{
				return EDesignRes.GetString("ProviderSchemaErrors");
			}
		}

		internal static string CodeGenSourceFilePathIsNotAFile
		{
			get
			{
				return EDesignRes.GetString("CodeGenSourceFilePathIsNotAFile");
			}
		}

		internal static string TargetEntityFrameworkVersionToNewForEntityClassGenerator
		{
			get
			{
				return EDesignRes.GetString("TargetEntityFrameworkVersionToNewForEntityClassGenerator");
			}
		}

		internal static string MissingDocumentationNoName
		{
			get
			{
				return EDesignRes.GetString("MissingDocumentationNoName");
			}
		}

		internal static string MetadataItemErrorsFoundDuringGeneration
		{
			get
			{
				return EDesignRes.GetString("MetadataItemErrorsFoundDuringGeneration");
			}
		}

		internal static string UnableToGenerateForeignKeyPropertiesForV1
		{
			get
			{
				return EDesignRes.GetString("UnableToGenerateForeignKeyPropertiesForV1");
			}
		}

		internal static string TypeComments
		{
			get
			{
				return EDesignRes.GetString("TypeComments");
			}
		}

		internal static string GetViewAtMethodComments
		{
			get
			{
				return EDesignRes.GetString("GetViewAtMethodComments");
			}
		}

		internal static string ConstructorComments
		{
			get
			{
				return EDesignRes.GetString("ConstructorComments");
			}
		}

		internal static string Template_CommentNoDocumentation
		{
			get
			{
				return EDesignRes.GetString("Template_CommentNoDocumentation");
			}
		}

		internal static string Template_GeneratedCodeCommentLine1
		{
			get
			{
				return EDesignRes.GetString("Template_GeneratedCodeCommentLine1");
			}
		}

		internal static string Template_GeneratedCodeCommentLine2
		{
			get
			{
				return EDesignRes.GetString("Template_GeneratedCodeCommentLine2");
			}
		}

		internal static string Template_GeneratedCodeCommentLine3
		{
			get
			{
				return EDesignRes.GetString("Template_GeneratedCodeCommentLine3");
			}
		}

		internal static string Template_RegionRelationships
		{
			get
			{
				return EDesignRes.GetString("Template_RegionRelationships");
			}
		}

		internal static string Template_RegionContexts
		{
			get
			{
				return EDesignRes.GetString("Template_RegionContexts");
			}
		}

		internal static string Template_RegionObjectSetProperties
		{
			get
			{
				return EDesignRes.GetString("Template_RegionObjectSetProperties");
			}
		}

		internal static string Template_RegionAddToMethods
		{
			get
			{
				return EDesignRes.GetString("Template_RegionAddToMethods");
			}
		}

		internal static string Template_RegionFunctionImports
		{
			get
			{
				return EDesignRes.GetString("Template_RegionFunctionImports");
			}
		}

		internal static string Template_RegionEntities
		{
			get
			{
				return EDesignRes.GetString("Template_RegionEntities");
			}
		}

		internal static string Template_RegionNavigationProperties
		{
			get
			{
				return EDesignRes.GetString("Template_RegionNavigationProperties");
			}
		}

		internal static string Template_RegionComplexTypes
		{
			get
			{
				return EDesignRes.GetString("Template_RegionComplexTypes");
			}
		}

		internal static string Template_RegionFactoryMethod
		{
			get
			{
				return EDesignRes.GetString("Template_RegionFactoryMethod");
			}
		}

		internal static string Template_RegionPrimitiveProperties
		{
			get
			{
				return EDesignRes.GetString("Template_RegionPrimitiveProperties");
			}
		}

		internal static string Template_RegionSimpleProperties
		{
			get
			{
				return EDesignRes.GetString("Template_RegionSimpleProperties");
			}
		}

		internal static string Template_RegionComplexProperties
		{
			get
			{
				return EDesignRes.GetString("Template_RegionComplexProperties");
			}
		}

		internal static string Template_RegionEnumTypes
		{
			get
			{
				return EDesignRes.GetString("Template_RegionEnumTypes");
			}
		}

		internal static string Template_RegionConstructors
		{
			get
			{
				return EDesignRes.GetString("Template_RegionConstructors");
			}
		}

		internal static string Template_RegionPartialMethods
		{
			get
			{
				return EDesignRes.GetString("Template_RegionPartialMethods");
			}
		}

		internal static string Template_ReplaceVsItemTemplateToken
		{
			get
			{
				return EDesignRes.GetString("Template_ReplaceVsItemTemplateToken");
			}
		}

		internal static string Template_CurrentlyRunningTemplate
		{
			get
			{
				return EDesignRes.GetString("Template_CurrentlyRunningTemplate");
			}
		}

		internal static string Template_UnsupportedSchema
		{
			get
			{
				return EDesignRes.GetString("Template_UnsupportedSchema");
			}
		}

		internal static string EdmSchemaNotValid
		{
			get
			{
				return EDesignRes.GetString("EdmSchemaNotValid");
			}
		}

		internal static string EntityCodeGenTargetTooLow
		{
			get
			{
				return EDesignRes.GetString("EntityCodeGenTargetTooLow");
			}
		}

		internal static string StonglyTypedAccessToNullValue(object p0, object p1)
		{
			return EDesignRes.GetString("StonglyTypedAccessToNullValue", new object[]
			{
				p0,
				p1
			});
		}

		internal static string NoPrimaryKeyDefined(object p0)
		{
			return EDesignRes.GetString("NoPrimaryKeyDefined", new object[]
			{
				p0
			});
		}

		internal static string InvalidTypeForPrimaryKey(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("InvalidTypeForPrimaryKey", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string CannotCreateEntityWithNoPrimaryKeyDefined(object p0)
		{
			return EDesignRes.GetString("CannotCreateEntityWithNoPrimaryKeyDefined", new object[]
			{
				p0
			});
		}

		internal static string TableReferencedByAssociationWasNotFound(object p0)
		{
			return EDesignRes.GetString("TableReferencedByAssociationWasNotFound", new object[]
			{
				p0
			});
		}

		internal static string TableReferencedByTvfWasNotFound(object p0)
		{
			return EDesignRes.GetString("TableReferencedByTvfWasNotFound", new object[]
			{
				p0
			});
		}

		internal static string UnsupportedDataType(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("UnsupportedDataType", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string UnsupportedDataTypeUnknownType(object p0, object p1)
		{
			return EDesignRes.GetString("UnsupportedDataTypeUnknownType", new object[]
			{
				p0,
				p1
			});
		}

		internal static string UnsupportedFunctionReturnDataType(object p0, object p1)
		{
			return EDesignRes.GetString("UnsupportedFunctionReturnDataType", new object[]
			{
				p0,
				p1
			});
		}

		internal static string UnsupportedFunctionParameterDataType(object p0, object p1, object p2, object p3)
		{
			return EDesignRes.GetString("UnsupportedFunctionParameterDataType", new object[]
			{
				p0,
				p1,
				p2,
				p3
			});
		}

		internal static string UnsupportedDataTypeForTarget(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("UnsupportedDataTypeForTarget", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string UnsupportedFunctionReturnDataTypeForTarget(object p0, object p1)
		{
			return EDesignRes.GetString("UnsupportedFunctionReturnDataTypeForTarget", new object[]
			{
				p0,
				p1
			});
		}

		internal static string UnsupportedFunctionParameterDataTypeForTarget(object p0, object p1, object p2, object p3)
		{
			return EDesignRes.GetString("UnsupportedFunctionParameterDataTypeForTarget", new object[]
			{
				p0,
				p1,
				p2,
				p3
			});
		}

		internal static string UnsupportedDbRelationship(object p0)
		{
			return EDesignRes.GetString("UnsupportedDbRelationship", new object[]
			{
				p0
			});
		}

		internal static string ParameterDirectionNotValid(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("ParameterDirectionNotValid", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string InvalidStringArgument(object p0)
		{
			return EDesignRes.GetString("InvalidStringArgument", new object[]
			{
				p0
			});
		}

		internal static string Serialization_UnknownGlobalItem(object p0)
		{
			return EDesignRes.GetString("Serialization_UnknownGlobalItem", new object[]
			{
				p0
			});
		}

		internal static string ReservedNamespace(object p0)
		{
			return EDesignRes.GetString("ReservedNamespace", new object[]
			{
				p0
			});
		}

		internal static string ColumnFacetValueOutOfRange(object p0, object p1, object p2, object p3, object p4, object p5)
		{
			return EDesignRes.GetString("ColumnFacetValueOutOfRange", new object[]
			{
				p0,
				p1,
				p2,
				p3,
				p4,
				p5
			});
		}

		internal static string AssociationMissingKeyColumn(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("AssociationMissingKeyColumn", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string InvalidNonStoreEntityContainer(object p0)
		{
			return EDesignRes.GetString("InvalidNonStoreEntityContainer", new object[]
			{
				p0
			});
		}

		internal static string ExcludedColumnWasAKeyColumnEntityIsInvalid(object p0, object p1)
		{
			return EDesignRes.GetString("ExcludedColumnWasAKeyColumnEntityIsInvalid", new object[]
			{
				p0,
				p1
			});
		}

		internal static string ExcludedColumnWasAKeyColumnEntityIsReadOnly(object p0, object p1)
		{
			return EDesignRes.GetString("ExcludedColumnWasAKeyColumnEntityIsReadOnly", new object[]
			{
				p0,
				p1
			});
		}

		internal static string ModelGeneration_UnGeneratableType(object p0)
		{
			return EDesignRes.GetString("ModelGeneration_UnGeneratableType", new object[]
			{
				p0
			});
		}

		internal static string DuplicateEntityContainerName(object p0, object p1)
		{
			return EDesignRes.GetString("DuplicateEntityContainerName", new object[]
			{
				p0,
				p1
			});
		}

		internal static string ProviderFactoryReturnedNullFactory(object p0)
		{
			return EDesignRes.GetString("ProviderFactoryReturnedNullFactory", new object[]
			{
				p0
			});
		}

		internal static string InvalidNamespaceNameArgument(object p0)
		{
			return EDesignRes.GetString("InvalidNamespaceNameArgument", new object[]
			{
				p0
			});
		}

		internal static string InvalidEntityContainerNameArgument(object p0)
		{
			return EDesignRes.GetString("InvalidEntityContainerNameArgument", new object[]
			{
				p0
			});
		}

		internal static string EntityClient_InvalidStoreProvider(object p0)
		{
			return EDesignRes.GetString("EntityClient_InvalidStoreProvider", new object[]
			{
				p0
			});
		}

		internal static string DbProviderServicesInformationLocationPath(object p0, object p1)
		{
			return EDesignRes.GetString("DbProviderServicesInformationLocationPath", new object[]
			{
				p0,
				p1
			});
		}

		internal static string UnsupportedForeignKeyPattern(object p0, object p1, object p2, object p3)
		{
			return EDesignRes.GetString("UnsupportedForeignKeyPattern", new object[]
			{
				p0,
				p1,
				p2,
				p3
			});
		}

		internal static string UnsupportedQueryViewInEntityContainerMapping(object p0)
		{
			return EDesignRes.GetString("UnsupportedQueryViewInEntityContainerMapping", new object[]
			{
				p0
			});
		}

		internal static string SharedForeignKey(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("SharedForeignKey", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string UnmappedFunctionImport(object p0)
		{
			return EDesignRes.GetString("UnmappedFunctionImport", new object[]
			{
				p0
			});
		}

		internal static string CannotChangePropertyReturnType(object p0, object p1)
		{
			return EDesignRes.GetString("CannotChangePropertyReturnType", new object[]
			{
				p0,
				p1
			});
		}

		internal static string CannotChangePropertyReturnTypeToNull(object p0, object p1)
		{
			return EDesignRes.GetString("CannotChangePropertyReturnTypeToNull", new object[]
			{
				p0,
				p1
			});
		}

		internal static string InvalidAttributeSuppliedForType(object p0)
		{
			return EDesignRes.GetString("InvalidAttributeSuppliedForType", new object[]
			{
				p0
			});
		}

		internal static string InvalidMemberSuppliedForType(object p0)
		{
			return EDesignRes.GetString("InvalidMemberSuppliedForType", new object[]
			{
				p0
			});
		}

		internal static string InvalidInterfaceSuppliedForType(object p0)
		{
			return EDesignRes.GetString("InvalidInterfaceSuppliedForType", new object[]
			{
				p0
			});
		}

		internal static string InvalidAttributeSuppliedForProperty(object p0)
		{
			return EDesignRes.GetString("InvalidAttributeSuppliedForProperty", new object[]
			{
				p0
			});
		}

		internal static string InvalidGetStatementSuppliedForProperty(object p0)
		{
			return EDesignRes.GetString("InvalidGetStatementSuppliedForProperty", new object[]
			{
				p0
			});
		}

		internal static string InvalidSetStatementSuppliedForProperty(object p0)
		{
			return EDesignRes.GetString("InvalidSetStatementSuppliedForProperty", new object[]
			{
				p0
			});
		}

		internal static string PropertyExistsWithDifferentCase(object p0)
		{
			return EDesignRes.GetString("PropertyExistsWithDifferentCase", new object[]
			{
				p0
			});
		}

		internal static string EntitySetExistsWithDifferentCase(object p0)
		{
			return EDesignRes.GetString("EntitySetExistsWithDifferentCase", new object[]
			{
				p0
			});
		}

		internal static string ItemExistsWithDifferentCase(object p0, object p1)
		{
			return EDesignRes.GetString("ItemExistsWithDifferentCase", new object[]
			{
				p0,
				p1
			});
		}

		internal static string NullAdditionalSchema(object p0, object p1)
		{
			return EDesignRes.GetString("NullAdditionalSchema", new object[]
			{
				p0,
				p1
			});
		}

		internal static string DuplicateClassName(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("DuplicateClassName", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string MissingPropertyDocumentation(object p0)
		{
			return EDesignRes.GetString("MissingPropertyDocumentation", new object[]
			{
				p0
			});
		}

		internal static string MissingComplexTypeDocumentation(object p0)
		{
			return EDesignRes.GetString("MissingComplexTypeDocumentation", new object[]
			{
				p0
			});
		}

		internal static string MissingDocumentation(object p0)
		{
			return EDesignRes.GetString("MissingDocumentation", new object[]
			{
				p0
			});
		}

		internal static string NamespaceComments(object p0, object p1)
		{
			return EDesignRes.GetString("NamespaceComments", new object[]
			{
				p0,
				p1
			});
		}

		internal static string FactoryMethodSummaryComment(object p0)
		{
			return EDesignRes.GetString("FactoryMethodSummaryComment", new object[]
			{
				p0
			});
		}

		internal static string FactoryParamCommentGeneral(object p0)
		{
			return EDesignRes.GetString("FactoryParamCommentGeneral", new object[]
			{
				p0
			});
		}

		internal static string CtorSummaryComment(object p0)
		{
			return EDesignRes.GetString("CtorSummaryComment", new object[]
			{
				p0
			});
		}

		internal static string EmptyCtorSummaryComment(object p0, object p1)
		{
			return EDesignRes.GetString("EmptyCtorSummaryComment", new object[]
			{
				p0,
				p1
			});
		}

		internal static string GeneratedNavigationPropertyNameConflict(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("GeneratedNavigationPropertyNameConflict", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string GeneratedPropertyAccessibilityConflict(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("GeneratedPropertyAccessibilityConflict", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string EntityTypeAndSetAccessibilityConflict(object p0, object p1, object p2, object p3)
		{
			return EDesignRes.GetString("EntityTypeAndSetAccessibilityConflict", new object[]
			{
				p0,
				p1,
				p2,
				p3
			});
		}

		internal static string GeneratedFactoryMethodNameConflict(object p0, object p1)
		{
			return EDesignRes.GetString("GeneratedFactoryMethodNameConflict", new object[]
			{
				p0,
				p1
			});
		}

		internal static string UnableToGenerateFunctionImportParameterName(object p0, object p1)
		{
			return EDesignRes.GetString("UnableToGenerateFunctionImportParameterName", new object[]
			{
				p0,
				p1
			});
		}

		internal static string IndividualViewComments(object p0)
		{
			return EDesignRes.GetString("IndividualViewComments", new object[]
			{
				p0
			});
		}

		internal static string TargetVersionSchemaVersionMismatch(object p0, object p1)
		{
			return EDesignRes.GetString("TargetVersionSchemaVersionMismatch", new object[]
			{
				p0,
				p1
			});
		}

		internal static string DuplicateEntryInUserDictionary(object p0, object p1)
		{
			return EDesignRes.GetString("DuplicateEntryInUserDictionary", new object[]
			{
				p0,
				p1
			});
		}

		internal static string UnsupportedLocaleForPluralizationServices(object p0)
		{
			return EDesignRes.GetString("UnsupportedLocaleForPluralizationServices", new object[]
			{
				p0
			});
		}

		internal static string Template_DuplicateTopLevelType(object p0)
		{
			return EDesignRes.GetString("Template_DuplicateTopLevelType", new object[]
			{
				p0
			});
		}

		internal static string Template_ConflictingGeneratedNavPropName(object p0, object p1, object p2)
		{
			return EDesignRes.GetString("Template_ConflictingGeneratedNavPropName", new object[]
			{
				p0,
				p1,
				p2
			});
		}

		internal static string Template_FactoryMethodNameConflict(object p0, object p1)
		{
			return EDesignRes.GetString("Template_FactoryMethodNameConflict", new object[]
			{
				p0,
				p1
			});
		}

		internal static string Template_CaseInsensitiveTypeConflict(object p0)
		{
			return EDesignRes.GetString("Template_CaseInsensitiveTypeConflict", new object[]
			{
				p0
			});
		}

		internal static string Template_CaseInsensitiveEntitySetConflict(object p0, object p1)
		{
			return EDesignRes.GetString("Template_CaseInsensitiveEntitySetConflict", new object[]
			{
				p0,
				p1
			});
		}

		internal static string Template_CaseInsensitiveMemberConflict(object p0, object p1)
		{
			return EDesignRes.GetString("Template_CaseInsensitiveMemberConflict", new object[]
			{
				p0,
				p1
			});
		}

		internal static string Template_GenCommentAddToMethodCs(object p0)
		{
			return EDesignRes.GetString("Template_GenCommentAddToMethodCs", new object[]
			{
				p0
			});
		}

		internal static string Template_GenCommentAddToMethodVb(object p0)
		{
			return EDesignRes.GetString("Template_GenCommentAddToMethodVb", new object[]
			{
				p0
			});
		}

		internal static string Template_CommentFactoryMethodParam(object p0)
		{
			return EDesignRes.GetString("Template_CommentFactoryMethodParam", new object[]
			{
				p0
			});
		}

		internal static string Template_ContextDefaultCtorComment(object p0, object p1)
		{
			return EDesignRes.GetString("Template_ContextDefaultCtorComment", new object[]
			{
				p0,
				p1
			});
		}

		internal static string Template_ContextCommonCtorComment(object p0)
		{
			return EDesignRes.GetString("Template_ContextCommonCtorComment", new object[]
			{
				p0
			});
		}

		internal static string Template_FactoryMethodComment(object p0)
		{
			return EDesignRes.GetString("Template_FactoryMethodComment", new object[]
			{
				p0
			});
		}

		internal static string EdmSchemaFileNotFound(object p0)
		{
			return EDesignRes.GetString("EdmSchemaFileNotFound", new object[]
			{
				p0
			});
		}

		internal static string DefaultTargetVersionTooLow(object p0, object p1)
		{
			return EDesignRes.GetString("DefaultTargetVersionTooLow", new object[]
			{
				p0,
				p1
			});
		}

		internal static string EntityClient_DoesNotImplementIServiceProvider(object p0)
		{
			return EDesignRes.GetString("EntityClient_DoesNotImplementIServiceProvider", new object[]
			{
				p0
			});
		}

		internal static string EntityClient_ReturnedNullOnProviderMethod(object p0, object p1)
		{
			return EDesignRes.GetString("EntityClient_ReturnedNullOnProviderMethod", new object[]
			{
				p0,
				p1
			});
		}
	}
}
