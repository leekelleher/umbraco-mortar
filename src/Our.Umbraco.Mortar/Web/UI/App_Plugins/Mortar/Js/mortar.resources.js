/* Resources */
angular.module('umbraco.resources').factory('Our.Umbraco.Mortar.Resources.mortarResources',
    function($q, $http, umbRequestHelper) {
        return {
            getContentTypeAlias: function (docTypeId) {
                var url = "/umbraco/backoffice/MortarApi/MortarApi/GetContentTypeAlias?docTypeId=" + docTypeId;
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve the content type alias'
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
                    'Failed to retrieve the content types'
                );
            },
            getDataTypePreValues: function (dtdId) {
                var url = "/umbraco/backoffice/MortarApi/MortarApi/GetDataTypePreValues?dtdid=" + dtdId;
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve the datatype prevalues'
                );
            },
            getDocTypePreview: function (docTypeId) {
                var url = "/umbraco/backoffice/MortarApi/MortarApi/GetDocTypePreview?docTypeId=" + docTypeId;
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve the content type preview'
                );
            },
        };
    });