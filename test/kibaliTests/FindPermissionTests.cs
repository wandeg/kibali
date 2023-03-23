﻿using Kibali;
using Xunit.Abstractions;

namespace KibaliTests
{
    public class FindPermissionTests
    {

        private readonly ITestOutputHelper _output;
        public FindPermissionTests(ITestOutputHelper output)
        {
            _output = output;
        }
        [Fact]
        public void FindCollection()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(CreatePermissionsDocument());

            var resource = authZChecker.FindResource("/foo");
            
            Assert.Equal("/foo",resource.Url);

        }

        [Fact]
        public void NegativeFindCollection()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(CreatePermissionsDocument());

            var resource = authZChecker.FindResource("/blahs");

            Assert.Null(resource);

        }

        [Fact]
        public void FindItem()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(CreatePermissionsDocument());

            var resource = authZChecker.FindResource("/bar/asdasd");

            Assert.Equal("/bar/{id}", resource.Url);

            Assert.Contains(resource.SupportedMethods["GET"]["DelegatedPersonal"], ac => ac.Permission == "Bar.Read");
            Assert.Contains(resource.SupportedMethods["GET"]["DelegatedWork"], ac => ac.Permission == "Foo.Read");
        }

        [Fact]
        public void FindRelated()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(CreatePermissionsDocument());

            var resource = authZChecker.FindResource("/bar/asdasd/schmo");

            Assert.Equal("/bar/{id}/schmo", resource.Url);
            Assert.Contains(resource.SupportedMethods["GET"]["Application"], ac => ac.Permission == "Foo.Read");
            Assert.Contains(resource.SupportedMethods["GET"]["DelegatedWork"], ac => ac.Permission == "Foo.Read");
        }

        [Fact]
        public void NegativeFindRelated()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(CreatePermissionsDocument());

            var resource = authZChecker.FindResource("/bar/asdasd/glib");

            Assert.Null(resource);
        }

        [Fact]
        public void FindRelatedBracesEnd()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(CreatePermissionsDocument());

            var resource = authZChecker.FindResource("/bar/asdasd/SCHMO/(value)");

            Assert.Equal("/bar/{id}/schmo", resource.Url);
            Assert.Contains(resource.SupportedMethods["GET"]["Application"], ac => ac.Permission == "Foo.Read");
            Assert.Contains(resource.SupportedMethods["GET"]["DelegatedWork"], ac => ac.Permission == "Foo.Read");
        }

        [Fact]
        public void FindRelatedBracesInfix()
        {
            var authZChecker = new AuthZChecker();
            authZChecker.Load(CreatePermissionsDocument());

            var resource = authZChecker.FindResource("/bar/(value)/asdasd/SchMO");

            Assert.Equal("/bar/{id}/schmo", resource.Url);
            Assert.Contains(resource.SupportedMethods["GET"]["Application"], ac => ac.Permission == "Foo.Read");
            Assert.Contains(resource.SupportedMethods["GET"]["DelegatedWork"], ac => ac.Permission == "Foo.Read");
        }

        private PermissionsDocument CreatePermissionsDocument()
        {
            var permissionsDocument = new PermissionsDocument();

            var fooRead = new Permission
            {
                PathSets = {
                        new PathSet() {
                            SchemeKeys = {
                                "DelegatedWork","Application"
                            },
                            Methods = {
                                "GET"
                            },
                            Paths = {
                                { "/foo",  null },
                                { "/bar",  null },
                                { "/bar/{id}",  null },
                                { "/bar/{id}/schmo",  null }
                            }
                        },
                        new PathSet() {
                            SchemeKeys = {
                                "DelegatedPersonal","Application"
                            },
                            Methods = {
                                "GET","POST"
                            },
                            Paths = {
                                { "/bar",  null },
                                { "/bar/{id}",  null },
                                { "/bar/{id}/schmo",  null }
                            }

                        }
                    }
            };
            permissionsDocument.Permissions.Add("Foo.Read", fooRead);

            var barRead = new Permission
            {
                PathSets = {
                        new PathSet() {
                            SchemeKeys = {
                                "DelegatedPersonal"
                            },
                            Methods = {
                                "GET"
                            },
                            Paths = {
                                { "/bar/{id}",  null },
                            }
                        }
                    }
            };
            permissionsDocument.Permissions.Add("Bar.Read", barRead);
            return permissionsDocument;
        }
    }
}
