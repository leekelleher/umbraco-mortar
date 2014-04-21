/* Controllers */
angular.module("umbraco").controller("Our.Umbraco.Mortar.Dialogs.docTypeDialog",
    [
        "$scope",
        "editorState",
        "contentResource",
        "contentTypeResource",
        "Our.Umbraco.Mortar.Resources.mortarResources",

        function ($scope, editorState, contentResource, contentTypeResource, mortarResources) {

            // Probably not best practise to talk to parent scope directly, but hey...
            $scope.model = $scope.$parent.$parent.model;

            console.log($scope);

            $scope.docTypes = [];
            $scope.dialogMode = "selectDocType";
            $scope.selectedDocType = null;
            $scope.node = null;

            $scope.selectDocType = function () {
                $scope.dialogMode = "edit";
                $scope.model.docType = $scope.selectedDocType.alias; // Maybe alias?
                loadNode();
            };

            $scope.save = function() {
                // Copy property values to scope model value
                if ($scope.node) {
                    var value = {};
                    for (var t = 0; t < $scope.node.tabs.length; t++) {
                        var tab = $scope.node.tabs[t];
                        for (var p = 0; p < tab.properties.length; p++) {
                            var prop = tab.properties[p];
                            value[prop.alias] = prop.value;
                        }
                    }
                    $scope.model.value = value;
                } else {
                    $scope.model.value = null;
                }

                $scope.submit($scope.dialogData);
            };

            function loadNode() {
                contentResource.getScaffold(-20, $scope.model.docType).then(function (data) {
                    // Remove the last tab
                    data.tabs.pop();

                    // Merge current value
                    if ($scope.model.value) {
                        for (var t = 0; t < data.tabs.length; t++) {
                            var tab = data.tabs[t];
                            for (var p = 0; p < tab.properties.length; p++) {
                                var prop = tab.properties[p];
                                if ($scope.model.value[prop.alias]) {
                                    prop.value = $scope.model.value[prop.alias];
                                }
                            }
                        }
                    };

                    // Assign the model to scope
                    $scope.node = data;

                    //
                    editorState.set($scope.node);
                });
            };


            if ($scope.model.docType) {
                $scope.dialogMode = "edit";
                loadNode();
            } else {
                $scope.dialogMode = "selectDocType";
                // No data type, so load a list to choose from
                mortarResources.getContentTypes().then(function (docTypes) {
                    $scope.docTypes = docTypes;
                });
            }

        }

    ]);
