﻿/* Controllers */
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
            $scope.dialogOptions = $scope.$parent.dialogOptions;

            $scope.docTypes = [];
            $scope.dialogMode = "selectDocType";
            $scope.selectedDocType = null;
            $scope.node = null;
            $scope.nameProperty = {
                hideLabel: false,
                alias: "name",
                label: "Name",
                description: "Give this piece content a name.",
                value: ""
            };

            $scope.selectDocType = function () {
                $scope.dialogMode = "edit";
                $scope.dialogData = $scope.selectedDocType.alias;
                loadNode();
            };

            $scope.save = function () {
                // Copy property values to scope model value
                if ($scope.node) {
                    var value = {
                        name: $scope.nameProperty.value
                    };
                    for (var t = 0; t < $scope.node.tabs.length; t++) {
                        var tab = $scope.node.tabs[t];
                        for (var p = 0; p < tab.properties.length; p++) {
                            var prop = tab.properties[p];
                            if (typeof prop.value !== "function") {
                                value[prop.alias] = prop.value;
                            }
                        }
                    }
                    $scope.dialogData.value = value;
                } else {
                    $scope.dialogData.value = null;
                }

                $scope.submit($scope.dialogData);
            };

            function loadNode() {
                contentResource.getScaffold(-20, $scope.dialogData.docType).then(function (data) {
                    // Remove the last tab
                    data.tabs.pop();

                    // Merge current value
                    if ($scope.dialogData.value) {
                        $scope.nameProperty.value = $scope.dialogData.value.name;
                        for (var t = 0; t < data.tabs.length; t++) {
                            var tab = data.tabs[t];
                            for (var p = 0; p < tab.properties.length; p++) {
                                var prop = tab.properties[p];
                                if ($scope.dialogData.value[prop.alias]) {
                                    prop.value = $scope.dialogData.value[prop.alias];
                                }
                            }
                        }
                    };

                    // Assign the model to scope
                    $scope.node = data;

                    editorState.set($scope.node);
                });
            };

            if ($scope.docTypes.length == 1) {
                $scope.dialogData.docType = $scope.docTypes[0];
            }

            if ($scope.dialogData.docType) {
                $scope.dialogMode = "edit";
                loadNode();
            } else {
                $scope.dialogMode = "selectDocType";
                // No data type, so load a list to choose from
                mortarResources.getContentTypes($scope.dialogOptions.allowedDocTypes).then(function(docTypes) {
                    $scope.docTypes = docTypes;
                });
            }

        }

    ]);

