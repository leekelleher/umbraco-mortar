/* Resources */
angular.module('umbraco.resources').factory('Our.Umbraco.Mortar.Resources.mortarResources',
    function($q, $http, umbRequestHelper) {
        return {
            getContentTypes: function() {
                return umbRequestHelper.resourcePromise(
                    $http.get("/umbraco/backoffice/MortarApi/MortarApi/GetContentTypes"),
                    'Failed to retrieve datatypes'
                );
            }
        };
    });