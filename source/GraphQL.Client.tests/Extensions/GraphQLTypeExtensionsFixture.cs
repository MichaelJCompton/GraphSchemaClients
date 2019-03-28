using System;
using System.Collections.Generic;
using Assent;
using Assent.Namers;
using FluentAssertions;
using GraphQL.Client.Attributes;
using GraphQL.Client.Extensions;
using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;
using NUnit.Framework;

namespace GraphQL.Client.tests.Extensions {
    public class GraphQLTypeExtensionsFixture {

        private Assent.Configuration SelectionSetAssentConfiguration;

        [OneTimeSetUp]
        public void SetupAssent() {
            SelectionSetAssentConfiguration = new Assent.Configuration()
                .UsingNamer(new SubdirectoryNamer("AsSelectionSetApproved"));
        }

        #region IsListGraphType

        [Test]
        public void ListsAreListGraphType() {
            var listType = new ListGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name));
            listType.IsListGraphType().Should().BeTrue();
        }

        [Test]
        public void NonNullListsAreListGraphType() {
            var nonnullListType = new NonNullGraphType(new ListGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name)));
            nonnullListType.IsListGraphType().Should().BeTrue();
        }

        [Test]
        public void NamedTypesAreNotListGraphType() {
            var refType = new GraphQLTypeReference(typeof(ATestGraphQLClass).Name);
            refType.IsListGraphType().Should().BeFalse();
        }

        #endregion

        #region GetCSNamedType

        [Test]
        public void GetCSNamedTypeReturnsClassType() {
            typeof(string).GetCSNamedType().Should().Be(typeof(string));

            typeof(ATestClass).GetCSNamedType().Should().Be(typeof(ATestClass));
        }

        [Test]
        public void GetCSNamedTypeReturnsListGenericType() {
            typeof(List<string>).GetCSNamedType().Should().Be(typeof(string));

            typeof(List<ATestClass>).GetCSNamedType().Should().Be(typeof(ATestClass));
        }

        #endregion

        #region IsGraphQLType

        [Test]
        public void ScalarsIsGraphQLType() {
            typeof(string).IsGraphQLType().Should().BeTrue();
            typeof(int).IsGraphQLType().Should().BeTrue();
            typeof(float).IsGraphQLType().Should().BeTrue();
            typeof(double).IsGraphQLType().Should().BeTrue();
            typeof(bool).IsGraphQLType().Should().BeTrue();
            typeof(DateTime).IsGraphQLType().Should().BeTrue();

            typeof(List<string>).IsGraphQLType().Should().BeTrue();
            typeof(List<int>).IsGraphQLType().Should().BeTrue();
            typeof(List<float>).IsGraphQLType().Should().BeTrue();
            typeof(List<double>).IsGraphQLType().Should().BeTrue();
            typeof(List<bool>).IsGraphQLType().Should().BeTrue();
            typeof(List<DateTime>).IsGraphQLType().Should().BeTrue();
        }

        [Test]
        public void ClassWithAttributeIsGraphQLType() {
            typeof(ATestGraphQLClass).IsGraphQLType().Should().BeTrue();

            typeof(List<ATestGraphQLClass>).IsGraphQLType().Should().BeTrue();
        }

        [Test]
        public void NoAttributeIsNotIsGraphQLType() {
            typeof(ATestClass).IsGraphQLType().Should().BeFalse();

            typeof(List<ATestClass>).IsGraphQLType().Should().BeFalse();
        }

        #endregion

        #region IsCompatibleType

        [Test]
        public void MatchingTypesAreCompatible() {
            (new GraphQLTypeReference(typeof(string).Name)).IsCompatibleType(typeof(string)).Should().BeTrue();
            (new GraphQLTypeReference("Int")).IsCompatibleType(typeof(int)).Should().BeTrue(); // because the type name is different
            (new GraphQLTypeReference("Float")).IsCompatibleType(typeof(float)).Should().BeTrue(); // because the C# type name is different
            (new GraphQLTypeReference("Float")).IsCompatibleType(typeof(double)).Should().BeTrue(); // because the C# type name is different
            (new GraphQLTypeReference(typeof(ATestGraphQLClass).Name)).IsCompatibleType(typeof(ATestGraphQLClass)).Should().BeTrue();

            (new NonNullGraphType(new GraphQLTypeReference(typeof(string).Name))).IsCompatibleType(typeof(string)).Should().BeTrue();
            (new NonNullGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name))).IsCompatibleType(typeof(ATestGraphQLClass)).Should().BeTrue();
        }

        [Test]
        public void IDCompatibleWithString() {
            (new GraphQLTypeReference("ID")).IsCompatibleType(typeof(string)).Should().BeTrue();

            (new NonNullGraphType(new GraphQLTypeReference("ID"))).IsCompatibleType(typeof(string)).Should().BeTrue();
        }

        [Test]
        public void ListGraphTypesAreCompatibleWithLists() {
            (new ListGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name))).IsCompatibleType(typeof(List<ATestGraphQLClass>)).Should().BeTrue();
            (new ListGraphType(new NonNullGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name)))).IsCompatibleType(typeof(List<ATestGraphQLClass>)).Should().BeTrue();
            (new NonNullGraphType((new ListGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name))))).IsCompatibleType(typeof(List<ATestGraphQLClass>)).Should().BeTrue();
            (new NonNullGraphType(new ListGraphType(new NonNullGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name))))).IsCompatibleType(typeof(List<ATestGraphQLClass>)).Should().BeTrue();
        }

        [Test]
        public void NonMatchingTypesAreNotCompatible() {
            (new GraphQLTypeReference(typeof(string).Name)).IsCompatibleType(typeof(int)).Should().BeFalse();
            (new GraphQLTypeReference("ID")).IsCompatibleType(typeof(int)).Should().BeFalse();
            (new GraphQLTypeReference(typeof(ATestGraphQLClass).Name)).IsCompatibleType(typeof(ATestClass)).Should().BeFalse();
            (new NonNullGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name))).IsCompatibleType(typeof(ATestClass)).Should().BeFalse();
            (new ListGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name))).IsCompatibleType(typeof(ATestClass)).Should().BeFalse();
        }

        #endregion

        #region ToIType

        [Test]
        public void RefTypesBecomeNamedTypes() {
            var refType = new GraphQLTypeReference(typeof(ATestGraphQLClass).Name);

            var itype = refType.ToIType();
            itype.GetType().Should().Be(typeof(NamedType));
            (itype as NamedType).Name.Should().Be(typeof(ATestGraphQLClass).Name);
        }

        [Test]
        public void ToITypeNonNullBase() {
            var nonnullType = new NonNullGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name));

            ((nonnullType.ToIType() as NonNullType)
                .Type as NamedType).Name.Should().Be(typeof(ATestGraphQLClass).Name);
        }

        [Test]
        public void ToITypeNonNullList() {
            var nonnullListType = new NonNullGraphType(new ListGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name)));

            (((nonnullListType.ToIType() as NonNullType).Type as ListType).Type as NamedType).Name.Should().Be(typeof(ATestGraphQLClass).Name);
        }

        [Test]
        public void ToITypeList() {
            var listType = new ListGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name));

            ((listType.ToIType() as ListType).Type as NamedType).Name.Should().Be(typeof(ATestGraphQLClass).Name);
        }

        [Test]
        public void ToITypeListNonNull() {
            var listNonnullType = new ListGraphType(new NonNullGraphType(new GraphQLTypeReference(typeof(ATestGraphQLClass).Name)));

            (((listNonnullType.ToIType() as ListType).Type as NonNullType).Type as NamedType).Name.Should().Be(typeof(ATestGraphQLClass).Name);
        }

        #endregion

        #region AsSelectionSet

        [Test]
        public void AsSelctionSetBase() {
            this.Assent(AstPrinter.Print(typeof(ATestGraphQLClass).AsSelctionSet(1)), SelectionSetAssentConfiguration);
        }

        [Test]
        public void AsSelctionSetNested() {
            this.Assent(AstPrinter.Print(typeof(ANestedGraphQLClass).AsSelctionSet(2)), SelectionSetAssentConfiguration);
        }

        [Test]
        public void AsSelctionSetDeepNested() {
            this.Assent(AstPrinter.Print(typeof(DeepNestedGraphQLClass).AsSelctionSet(3)), SelectionSetAssentConfiguration);
        }

        #endregion

        private class ATestClass {
            public int AnInt { get; set; }
        }

        [GraphQLModel]
        private class ATestGraphQLClass {
            public int AnInt { get; set; }
            public float AFloat { get; set; }
            public double ADouble { get; set; }
            public DateTime TheTime { get; set; }
        }

        [GraphQLModel]
        private class ANestedGraphQLClass {
            public int AnInt { get; set; }
            public ATestGraphQLClass Edge { get; set; }
            public ATestClass NotEdge { get; set; }
        }

        [GraphQLModel]
        private class DeepNestedGraphQLClass {
            public int AnInt { get; set; }
            public ATestGraphQLClass Edge { get; set; }
            public ANestedGraphQLClass NestedEdge { get; set; }
            public DeepNestedGraphQLClass DeepEdge { get; set; }
        }
    }
}