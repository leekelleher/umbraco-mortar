/* Resources */
angular.module('umbraco.resources').factory('Our.Umbraco.Mortar.Resources.mortarResources',
    function($q, $http, umbRequestHelper) {
        return {
            getContentAliasByGuid: function (guid) {
                var url = "/umbraco/backoffice/MortarApi/MortarApi/GetContentTypeAliasByGuid?guid=" + guid;
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve datatype alias by guid'
                );
            },
            getContentTypes: function (allowedContentTypes) {
                var url = "/umbraco/backoffice/MortarApi/MortarApi/GetContentTypes";
                if (allowedContentTypes) {
                    for (var i = 0; i < allowedContentTypes.length; i++) {
                        url += (i == 0 ? "?" : "&") + "allowedContentTypes=" + allowedContentTypes[i];
                    }
                }
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve datatypes'
                );
            },
            getDataTypePreValues: function (dtdId) {
                var url = "/umbraco/backoffice/MortarApi/MortarApi/GetDataTypePreValues?dtdid=" + dtdId;
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve datatypes'
                );
            }
        };
    });