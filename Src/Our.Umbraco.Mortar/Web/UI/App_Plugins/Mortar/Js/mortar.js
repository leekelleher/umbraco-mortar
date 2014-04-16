/* Directives */
angular.module("umbraco.directives").directive('mortarLayout',
    function($compile, $routeParams, dialogService, notificationsService, entityResource) {

        var link = function($scope, element, attrs, ctrl) {

            // Setup scope
            $scope.model = $scope.model || {};
            $scope.model.cellIds = [];
            $scope.model.value = $scope.model.value || {};

            console.log($scope.model.value);

            $scope.addSubLayout = function(cellId, layout) {

                // Make sure we can add
                var td = $("#mortar-" + $scope.model.id + " td#" + cellId);
                if (typeof td.attr("data-max") === "undefined"
                    || typeof $scope.model.value[cellId] === "undefined"
                    || td.data("max") > $scope.model.value[cellId].length) {

                    // Go ahead, make my day
                    var itm = { layout: layout, items: new Array(layout.split(',').length) };
                    if (typeof $scope.model.value[cellId] === "undefined") {
                        $scope.model.value[cellId] = [itm];
                    } else {
                        $scope.model.value[cellId].push(itm);
                    }

                } else {
                    
                    // Oops, too many items
                    notificationsService.error("Container already has the maximum number of items allowed.");
                }
            };

            $scope.hasItems = function(cellId) {
                return typeof $scope.model.value[cellId] !== "undefined"
                    && $scope.model.value[cellId].length > 0;
            };

            // Convert the template into an angular template
            var template = $("<div />").append($scope.model.config.layout);

            template.find("td[id]").each(function (idx, itm) {
                var td = $(itm);
                var id = td.attr("id");

                // Store known cell ids
                $scope.model.cellIds.push(id);

                // Setup sub layout buttons
                var subLayouts = [["100"]];
                if (td.data("subLayouts") !== undefined) {
                    var layouts = td.data("subLayouts").split('|');
                    for (var i = 0; i < layouts.length; i++) {
                        subLayouts.push(layouts[i].split(','));
                    }
                }

                var subLayoutsContainer = $("<div class='sub-layout-options' />");
                for (var i = 0; i < subLayouts.length; i++) {
                    var lnk = $("<a class='sub-layout-option' ng-click=\"addSubLayout('" + id + "', '" + subLayouts[i].join() + "')\" prevent-default />");
                    var tbl = $("<table />");
                    var tr = $("<tr />");
                    for (var j = 0; j < subLayouts[i].length; j++) {
                        tr.append($("<td width='" + subLayouts[i][j] + "%' />"));
                    }
                    tbl.append(tr);
                    lnk.append(tbl);
                    subLayoutsContainer.append(lnk);
                };

                // Add controls to template
                td.addClass("enabled")
                    .append("<div data-id='" + id + "' class='sub-layouts' ng-show=\"hasItems('" + id + "')\">" +
                        "<mortar-sub-layout ng-repeat=\"row in model.value['" + id + "']\" />" +
                        "</div>")
                    .append(subLayoutsContainer);
            });

            // Compile the template
            var templateEl = angular.element(template.html());
            var compiled = $compile(templateEl);

            // Attach the HTML
            element.append(templateEl);

            // Set template
            compiled($scope);

        };

        return {
            restrict: "E",
            rep1ace: true,
            link: link
        };

    });

angular.module("umbraco.directives").directive('mortarSubLayout',
    function($compile, $routeParams, dialogService, notificationsService, entityResource) {

        var link = function($scope, element, attrs, ctrl) {

            $scope.model = $scope.row;

            $scope.hasValue = function (cellIndex) {
                var value =  $scope.model.items != undefined &&
                    $scope.model.items[cellIndex] != undefined &&
                    $scope.model.items[cellIndex] != null;
                return value;
            };

            $scope.setCellType = function (cellIndex, type) {
                $scope.model.items = $scope.model.items || new Array($scope.model.layout.split(',').length);
                switch(type) {
                    case "richtext":
                        $scope.model.items[cellIndex] = {
                            type: type,
                            value: ""
                        };
                        break;
                    case "link":
                        //TODO: Set start node
                        dialogService.contentPicker({
                            callback: function (data) {
                                $scope.model.items[cellIndex] = {
                                    type: type,
                                    value: data.id
                                };
                            }
                        });
                        break;
                    case "doctype":
                        //TODO: Show dialog
                        break;
                }
            };

            var subLayout = $scope.model.layout.split(',');

            // Convert the template into an angular template
            var template = $("<div />");

            var tbl = $("<table class='sub-layout' />");
            var tr = $("<tr />");
            for (var j = 0; j < subLayout.length; j++) {
                tr.append($("<td width='" + subLayout[j] + "%'>" +
                    "<div class='brick-select' ng-hide=\"hasValue("+ j +")\">" +
                    "<a href='#' ng-click=\"setCellType('" + j + "','richtext')\" prevent-default><i class='icon-edit' /></a>" +
                    "<a href='#' ng-click=\"setCellType('" + j + "','link')\" prevent-default><i class='icon-link' /></a>" +
                    "<a href='#' ng-click=\"setCellType('" + j + "','doctype')\" prevent-default><i class='icon-settings' /></a>" +
                    "</div>" +
                    "<mortar-item model='model.items[" + j + "]' />" +
                    "</td>"));
            }
            tbl.append(tr);
            template.append(tbl);

            // Compile the template
            var templateEl = angular.element(template.html());
            var compiled = $compile(templateEl);

            // Attach the HTML
            element.append(templateEl);

            // Set template
            compiled($scope);

        };

        return {
            restrict: "E",
            rep1ace: true,
            link: link
        };

    });

/* 
 The role of the mortarItem is to delegate the
 creation of the correct mortar item type
*/
angular.module("umbraco.directives").directive('mortarItem',
    function($compile, $routeParams, dialogService, notificationsService, entityResource) {

        var link = function ($scope, element, attrs, ctrl) {

            $scope.$watch("model", function (newValue, oldValue) {

                // Remove current item
                element.empty();

                //TODO: Not sure whether we need to do anything to cleanup scope?

                // Add new item
                if (newValue !== undefined && newValue !== null) {
                    var el = $compile("<mortar-" + newValue.type + "-item model='model'></test>")($scope);
                    element.append(el);
                }

            });

        };

        return {
            restrict: "E",
            rep1ace: true,
            scope: {
              model: '='  
            },
            link: link
        };

    });

angular.module("umbraco.directives").directive('mortarLinkItem',
    function ($compile, $routeParams, dialogService, notificationsService, entityResource) {

        var link = function ($scope, element, attrs, ctrl) {

            // Setup model
            $scope.node = {};

            // Grab the node from the id
            entityResource.getById($scope.model.value, "Document").then(function (data) {
                $scope.node = data;
            });

        };

        return {
            restrict: "E",
            rep1ace: true,
            template: "<div class='mortar-item--link'><i class='icon-link'></i> {{node.name}}</div>",
            scope: {
                model: '='
            },
            link: link
        };

    });

angular.module("umbraco.directives").directive('mortarRichtextItem',
    function ($q, $timeout, tinyMceService, assetsService) {

        var link = function ($scope, element, attrs, ctrl) {

            $scope.model.alias = "test";

            tinyMceService.configuration().then(function (tinyMceConfig) {

                //config value from general tinymce.config file
                var validElements = tinyMceConfig.validElements;

                //These are absolutely required in order for the macros to render inline
                //we put these as extended elements because they get merged on top of the normal allowed elements by tiny mce
                var extendedValidElements = "@[id|class|style],-div[id|dir|class|align|style],ins[datetime|cite],-ul[class|style],-li[class|style]";

                var invalidElements = tinyMceConfig.inValidElements;
                var plugins = _.map(tinyMceConfig.plugins, function (plugin) {
                    if (plugin.useOnFrontend) {
                        return plugin.name;
                    }
                }).join(" ");

                //var editorConfig = $scope.model.config.editor;
                //if (!editorConfig || angular.isString(editorConfig)) {
                    editorConfig = tinyMceService.defaultPrevalues();
                //}

                //config value on the data type
                var toolbar = editorConfig.toolbar.join(" | ");
                var stylesheets = [];
                var styleFormats = [];
                var await = [];

                //queue file loading
                await.push(assetsService.loadJs("lib/tinymce/tinymce.min.js", $scope));

                //queue rules loading
                angular.forEach(editorConfig.stylesheets, function (val, key) {
                    stylesheets.push("../css/" + val + ".css?" + new Date().getTime());

                    await.push(stylesheetResource.getRulesByName(val).then(function (rules) {
                        angular.forEach(rules, function (rule) {
                            var r = {};
                            r.title = rule.name;
                            if (rule.selector[0] == ".") {
                                r.inline = "span";
                                r.classes = rule.selector.substring(1);
                            }
                            else if (rule.selector[0] == "#") {
                                r.inline = "span";
                                r.attributes = { id: rule.selector.substring(1) };
                            }
                            else {
                                r.block = rule.selector;
                            }

                            styleFormats.push(r);
                        });
                    }));
                });


                //stores a reference to the editor
                var tinyMceEditor = null;

                //wait for queue to end
                $q.all(await).then(function () {


                    //create a baseline Config to exten upon
                    var baseLineConfigObj = {
                        mode: "exact",
                        skin: "umbraco",
                        plugins: plugins,
                        valid_elements: validElements,
                        invalid_elements: invalidElements,
                        extended_valid_elements: extendedValidElements,
                        menubar: false,
                        statusbar: false,
                        height: 200,
                        width: editorConfig.dimensions.width,
                        toolbar: toolbar,
                        content_css: stylesheets.join(','),
                        relative_urls: false,
                        style_formats: styleFormats
                    };


                    if (tinyMceConfig.customConfig) {
                        angular.extend(baseLineConfigObj, tinyMceConfig.customConfig);
                    }

                    //set all the things that user configs should not be able to override
                    baseLineConfigObj.elements = $scope.model.alias + "_rte";
                    baseLineConfigObj.setup = function (editor) {

                        //set the reference
                        tinyMceEditor = editor;

                        //enable browser based spell checking
                        editor.on('init', function (e) {
                            editor.getBody().setAttribute('spellcheck', true);
                        });

                        //We need to listen on multiple things here because of the nature of tinymce, it doesn't 
                        //fire events when you think!
                        //The change event doesn't fire when content changes, only when cursor points are changed and undo points
                        //are created. the blur event doesn't fire if you insert content into the editor with a button and then 
                        //press save. 
                        //We have a couple of options, one is to do a set timeout and check for isDirty on the editor, or we can 
                        //listen to both change and blur and also on our own 'saving' event. I think this will be best because a 
                        //timer might end up using unwanted cpu and we'd still have to listen to our saving event in case they clicked
                        //save before the timeout elapsed.
                        editor.on('change', function (e) {
                            angularHelper.safeApply($scope, function () {
                                $scope.model.value = editor.getContent();
                            });
                        });

                        editor.on('blur', function (e) {
                            angularHelper.safeApply($scope, function () {
                                $scope.model.value = editor.getContent();
                            });
                        });

                        //Create the insert media plugin
                        tinyMceService.createMediaPicker(editor, $scope);

                        //Create the embedded plugin
                        tinyMceService.createInsertEmbeddedMedia(editor, $scope);

                        //Create the insert link plugin
                        tinyMceService.createLinkPicker(editor, $scope);

                        //Create the insert macro plugin
                        tinyMceService.createInsertMacro(editor, $scope);
                    };




                    /** Loads in the editor */
                    function loadTinyMce() {

                        //we need to add a timeout here, to force a redraw so TinyMCE can find
                        //the elements needed
                        $timeout(function () {
                            console.log(baseLineConfigObj);
                            tinymce.DOM.events.domLoaded = true;
                            tinymce.init(baseLineConfigObj);
                        }, 200, false);
                    }




                    loadTinyMce();

                    //here we declare a special method which will be called whenever the value has changed from the server
                    //this is instead of doing a watch on the model.value = faster
                    $scope.model.onValueChanged = function (newVal, oldVal) {
                        //update the display val again if it has changed from the server;
                        tinyMceEditor.setContent(newVal, { format: 'raw' });
                        //we need to manually fire this event since it is only ever fired based on loading from the DOM, this
                        // is required for our plugins listening to this event to execute
                        tinyMceEditor.fire('LoadContent', null);
                    };

                    //listen for formSubmitting event (the result is callback used to remove the event subscription)
                    var unsubscribe = $scope.$on("formSubmitting", function () {

                        //TODO: Here we should parse out the macro rendered content so we can save on a lot of bytes in data xfer
                        // we do parse it out on the server side but would be nice to do that on the client side before as well.
                        $scope.model.value = tinyMceEditor.getContent();
                    });

                    //when the element is disposed we need to unsubscribe!
                    // NOTE: this is very important otherwise if this is part of a modal, the listener still exists because the dom 
                    // element might still be there even after the modal has been hidden.
                    $scope.$on('$destroy', function () {
                        unsubscribe();
                    });
                });
            });

        };

        return {
            restrict: "E",
            rep1ace: true,
            template: "<div class='mortar-item--richtext'><textarea ng-model=\"model.value\" rows=\"10\" id=\"{{model.alias}}_rte\"></textarea></div>",
            scope: {
                model: '='
            },
            link: link
        };

    });