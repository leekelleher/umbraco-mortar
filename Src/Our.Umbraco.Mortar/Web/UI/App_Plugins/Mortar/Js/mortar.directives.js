/* Directives */
angular.module("umbraco.directives").directive('mortarLayout',
    function ($timeout, $compile, $routeParams, dialogService, notificationsService) {

        var link = function ($scope, element, attrs, ctrl) {

            // Setup scope
            $scope.model = $scope.model || {};
            $scope.model.cellIds = [];
            $scope.model.value = $scope.model.value || {};
            $scope.model.layoutConfig = $scope.model.config.gridConfig;

            // Merge in the defauult config
            if (typeof $scope.model.config.defaultConfig !== "undefined") {
                for (var key in $scope.model.layoutConfig) {
                    if (typeof $scope.model.layoutConfig[key] === 'object') {
                        $scope.model.layoutConfig[key] = angular.extend({},
                            $scope.model.config.defaultConfig,
                            $scope.model.layoutConfig[key]);
                    }
                }
            }

            $scope.removeRow = function (cellId, index) {
                $scope.model.value[cellId].splice(index, 1);
            };

            $scope.addRow = function (cellId, layout) {

                // See if we have a max items config
                var maxItems = 0;
                if (cellId in $scope.model.layoutConfig && "maxItems" in $scope.model.layoutConfig[cellId]) {
                    maxItems = $scope.model.layoutConfig[cellId].maxItems;
                }

                // Enforce mac items
                if (maxItems == 0
                    || typeof $scope.model.value[cellId] === "undefined"
                    || maxItems > $scope.model.value[cellId].length) {

                    // Go ahead, make my day
                    var itm = { layout: layout, items: new Array(layout.split(',').length) };
                    if (typeof $scope.model.value[cellId] === "undefined") {
                        $scope.model.value[cellId] = [itm];
                    } else {
                        $scope.model.value[cellId].push(itm);
                    }
                    makeRowsSortable();

                } else {

                    // Oops, too many items
                    notificationsService.error("Container already has the maximum number of items allowed.");
                }
            };

            $scope.rowHasItems = function (cellId) {
                return typeof $scope.model.value[cellId] !== "undefined"
                    && $scope.model.value[cellId].length > 0;
            };

            $scope.rowHasMaxItems = function (cellId) {

                var maxItems = 0;
                if (cellId in $scope.model.layoutConfig && "maxItems" in $scope.model.layoutConfig[cellId]) {
                    maxItems = $scope.model.layoutConfig[cellId].maxItems;
                }

                return !(maxItems == 0
                    || typeof $scope.model.value[cellId] === "undefined"
                    || maxItems > $scope.model.value[cellId].length);
            };

            // Setup sorting
            var makeRowsSortable = function () {

                $("#mortar-" + $scope.model.id + " .mortar-rows.ui-sortable").sortable("destroy");
                $("#mortar-" + $scope.model.id + " .mortar-rows")
                    .sortable({
                        containment: "parent",
                        handle: ".mortar-row__sort",
                        start: function (e, ui) {
                            ui.item.data('start-index', ui.item.index());
                            $scope.$broadcast("mortar_sorting");
                        },
                        stop: function (e, ui) {

                            $scope.$broadcast("mortar_sorted");

                            var cell = $(ui.item).closest("td[id]").attr("id");

                            // Remove previous item
                            var startIndex = $(ui.item).data("start-index");
                            var itm = $scope.model.value[cell].splice(startIndex, 1);

                            // Add to new location
                            var targetIndex = ui.item.index();

                            $scope.model.value[cell].splice(targetIndex, 0, itm[0]);

                            // Update scope
                            $scope.$apply();
                        }
                    });

            };

            // Convert the template into an angular template
            var template = $("<div />").append($scope.model.config.gridLayout);

            template.find("td[id]").each(function (idx, itm) {
                var td = $(itm);
                var id = td.attr("id");

                // Store known cell ids
                $scope.model.cellIds.push(id);

                // Setup row layout buttons
                var rowLayouts = [[100]];
                if (id in $scope.model.layoutConfig && "layouts" in $scope.model.layoutConfig[id]) {
                    rowLayouts = $scope.model.layoutConfig[id].layouts;
                }

                var rowLayoutsContainer = $("<div class='row-layout-options' ng-hide=\"rowHasMaxItems('" + id + "')\" />");
                for (var i = 0; i < rowLayouts.length; i++) {
                    var lnk = $("<a class='row-layout-option' ng-click=\"addRow('" + id + "', '" + rowLayouts[i].join() + "')\" prevent-default />");
                    var tbl = $("<table />");
                    var tr = $("<tr />");
                    for (var j = 0; j < rowLayouts[i].length; j++) {
                        tr.append($("<td width='" + rowLayouts[i][j] + "%' />"));
                    }
                    tbl.append(tr);
                    lnk.append(tbl);
                    rowLayoutsContainer.append(lnk);
                };

                // Add controls to template
                td.addClass("enabled")
                    .append("<div data-id='" + id + "' class='mortar-rows' ng-show=\"rowHasItems('" + id + "')\">" +
                        "<mortar-row ng-repeat=\"item in model.value['" + id + "']\" model=\"item\" cell-id=\"" + id + "\" layout-config=\"model.layoutConfig['" + id + "']\" />" +
                        "</div>")
                    .append(rowLayoutsContainer);
            });

            // Compile the template
            var templateEl = angular.element(template.html());
            var compiled = $compile(templateEl);

            // Attach the HTML
            element.append(templateEl);

            // Set template
            compiled($scope);

            // Apply sort functionality
            $timeout(makeRowsSortable, 100);
        };

        return {
            restrict: "E",
            replace: true,
            template: "<div class='mortar-layout' />",
            link: link
        };

    });

angular.module("umbraco.directives").directive('mortarRow',
    [
        "$compile",
        "Our.Umbraco.Mortar.Services.docTypeDialogService",
        function ($compile, docTypeDialogService) {

            var link = function ($scope, element, attrs, ctrl) {

                $scope.model.options = $scope.model.options || {};

                $scope.canShowOptions = function () {
                    return "rowOptionsDocType" in $scope.layoutConfig && $scope.layoutConfig.rowOptionsDocType;
                };

                $scope.showOptions = function () {
                    docTypeDialogService.open({
                        requireName: false,
                        allowedDocTypes: [$scope.layoutConfig.rowOptionsDocType],
                        dialogData: {
                            value: $scope.model.options
                        },
                        callback: function (data) {
                            $scope.model.options = data.value;
                        }
                    });
                };

                $scope.isAllowed = function (contentType) {
                    return !$scope.layoutConfig.allowedContentTypes || $.grep($scope.layoutConfig.allowedContentTypes, function (itm, idx) {
                        return itm.toLowerCase() == contentType.toLowerCase();
                    }).length > 0;
                };

                $scope.hasValue = function (cellIndex) {
                    var value = $scope.model.items != undefined &&
                        $scope.model.items[cellIndex] != undefined &&
                        $scope.model.items[cellIndex] != null;
                    return value;
                };

                $scope.setCellType = function (cellIndex, type) {
                    $scope.model.items = $scope.model.items || new Array($scope.model.layout.split(',').length);
                    $scope.model.items[cellIndex] = {
                        type: type,
                        value: "-1" // We set to -1 so that directives know they are being created as a result of a click, and can peform specific init code
                    };
                };

                var rowLayout = $scope.model.layout.split(',');

                // Convert the template into an angular template
                var template = $("<div />");

                // Create the toolbar
                template.append($("<div class='mortar-row__button-bar mortar-button-bar mortar-button-bar--vertical mortar-button-bar--tl'>" +
                    "<a href='#' ng-click=\"$parent.removeRow(cellId, $parent.$index)\" prevent-default><i class='icon-delete' /></a>" +
                    "<a href='#' class='mortar-row__options' ng-click=\"showOptions()\" ng-show='canShowOptions()' prevent-default><i class='icon-settings' /></a>" +
                    "<a href='#' class='mortar-row__sort' ng-show='$parent.model.value[cellId].length > 1' prevent-default><i class='icon-list' /></a>" +
                    "</div>"));

                // Create the table
                var tbl = $("<table />");
                var tr = $("<tr />");
                for (var j = 0; j < rowLayout.length; j++) {
                    tr.append($("<td width='" + rowLayout[j] + "%'>" +
                        "<div class='mortar-row__cell'>" +
                        "<div class='mortar-button-bar mortar-button-bar--horizontal mortar-button-bar--tr' ng-hide=\"hasValue(" + j + ")\">" +
                        "<a href='#' ng-click=\"setCellType('" + j + "','richtext')\" ng-show=\"isAllowed('richtext')\" prevent-default><i class='icon-edit' /></a>" +
                        "<a href='#' ng-click=\"setCellType('" + j + "','link')\" ng-show=\"isAllowed('link')\" prevent-default><i class='icon-link' /></a>" +
                        "<a href='#' ng-click=\"setCellType('" + j + "','media')\" ng-show=\"isAllowed('media')\" prevent-default><i class='icon-picture' /></a>" +
                        "<a href='#' ng-click=\"setCellType('" + j + "','embed')\" ng-show=\"isAllowed('embed')\" prevent-default><i class='icon-display' /></a>" +
                        "<a href='#' ng-click=\"setCellType('" + j + "','docType')\" ng-show=\"isAllowed('docType')\" prevent-default><i class='icon-code' /></a>" +
                        "</div>" +
                        "<div class='mortar-row__cell-spacer' ng-hide=\"hasValue(" + j + ")\" />" +
                        "<mortar-item model='model.items[" + j + "]' layout-config='layoutConfig' />" +
                        "</div>" +
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
                replace: true,
                transclude: true,
                template: "<div class='mortar-rows__item mortar-row' />",
                link: link,
                scope: {
                    model: '=',
                    cellId: '@',
                    layoutConfig: '=',
                }
            };

        }
    ]);

/* 
 The role of the mortarItem is to delegate the
 creation of the correct mortar item type
*/
angular.module("umbraco.directives").directive('mortarItem',
    function ($compile, $routeParams, dialogService, notificationsService, entityResource) {

        var link = function ($scope, element, attrs, ctrl) {

            $scope.$watch("model", function (newValue, oldValue) {

                // Remove current item
                element.empty();

                // Add new item
                if (newValue !== undefined && newValue !== null) {

                    // Temporary fix because we renamed 'doctype' to 'docType'
                    if (newValue.type == "doctype" || newValue.type == "doc-type")
                        newValue.type = "docType";

                    // Convert pascal case to hyphenated string
                    var name = newValue.type.replace(/[a-z][A-Z]/g, function (str, offset) {
                        return str[0] + '-' + str[1].toLowerCase();
                    });

                    // Render the item
                    var el = $compile("<mortar-" + name + "-item model='model' layout-config='layoutConfig' />")($scope);
                    element.append(el);
                }

            });

        };

        return {
            restrict: "E",
            replace: true,
            template: "<div class='mortar-item' />",
            scope: {
                model: '=',
                layoutConfig: '=',
            },
            link: link
        };

    });

angular.module("umbraco.directives").directive('mortarRichtextItem',
    [
        "$q",
        "$timeout",
        "tinyMceService",
        "assetsService",
        "angularHelper",
        "stylesheetResource",
        "Our.Umbraco.Mortar.Resources.mortarResources",
        function ($q, $timeout, tinyMceService, assetsService, angularHelper, stylesheetResource, mortarResources) {

            var guid = function () {
                function _p8(s) {
                    var p = (Math.random().toString(16) + "000000000").substr(2, 8);
                    return s ? "-" + p.substr(0, 4) + "-" + p.substr(4, 4) : p;
                }
                return _p8() + _p8(true) + _p8(true) + _p8();
            };

            var link = function ($scope, element, attrs, ctrl) {

                $scope.guid = guid();

                $scope.remove = function () {
                    $scope.model = null;
                };

                $scope.toggleToolbar = function () {
                    var toolbar = element.find(".mce-toolbar");
                    if (!toolbar.is(":visible")) {
                        toolbar.slideDown("fast");
                    } else {
                        toolbar.slideUp("fast");
                    }
                };

                $scope.$on("mortar_sorting", function () {
                    element.find('.mortar_rte').each(function () {
                        tinymce.execCommand('mceRemoveEditor', false, $(this).attr('id'));
                        $(this).css("visibility", "hidden");
                    });
                });

                $scope.$on("mortar_sorted", function () {
                    element.find('.mortar_rte').each(function () {
                        tinymce.execCommand('mceAddEditor', true, $(this).attr('id'));
                        $(this).css("visibility", "visible");
                    });
                });

                var rteDtdId = "ca90c950-0aff-4e72-b976-a30b1ac57dad"; // Default RTE
                if ($scope.layoutConfig && "rteDtdId" in $scope.layoutConfig) {
                    rteDtdId = $scope.layoutConfig.rteDtdId;
                }

                if ($scope.model.value && $scope.model.value == "-1") {
                    // We don't need to initialize anything as a result of a click,
                    // so just set the value to empty
                    $scope.model.value = "";
                };

                mortarResources.getDataTypePreValues(rteDtdId).then(function (rteConfig) {

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

                        var editorConfig = null;
                        if ("editor" in rteConfig) {
                            editorConfig = rteConfig.editor;
                        }
                        if (!editorConfig || angular.isString(editorConfig)) {
                            editorConfig = tinyMceService.defaultPrevalues();
                        }

                        //config value on the data type
                        var toolbar = editorConfig.toolbar.join(" | ");
                        var stylesheets = [];
                        var styleFormats = [];
                        var await = [];

                        //queue file loading
                        if (typeof tinymce === "undefined") {
                            await.push(assetsService.loadJs("lib/tinymce/tinymce.min.js", $scope));
                        }

                        if (typeof $.fn.hoverIntent === "undefined") {
                            await.push(assetsService.loadJs("/App_Plugins/mortar/js/jquery.hoverIntent.minified.js", $scope));
                        }

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
                                    } else if (rule.selector[0] == "#") {
                                        r.inline = "span";
                                        r.attributes = { id: rule.selector.substring(1) };
                                    } else {
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
                                height: "100%",
                                width: "100%",
                                toolbar: toolbar,
                                content_css: stylesheets.join(','),
                                relative_urls: false,
                                style_formats: styleFormats,
                            };

                            if (tinyMceConfig.customConfig) {
                                angular.extend(baseLineConfigObj, tinyMceConfig.customConfig);
                            }

                            //set all the things that user configs should not be able to override
                            baseLineConfigObj.elements = "mortar_rte_" + $scope.guid;
                            baseLineConfigObj.setup = function (editor) {

                                //set the reference
                                tinyMceEditor = editor;

                                // Initialize the editor
                                editor.on('init', function (e) {

                                    // Move toolbar to bottom
                                    $(editor.editorContainer).find(".mce-toolbar-grp")
                                        .insertAfter($(editor.editorContainer).find(".mce-edit-area"));

                                    // Hookup hover
                                    $(editor.editorContainer).find(".mce-toolbar-grp").hoverIntent({
                                        over: function () {
                                            $(this).find(".mce-toolbar").slideDown("fast");
                                        },
                                        out: function () {
                                            $(this).find(".mce-toolbar").slideUp("fast");
                                        },
                                        interval: 50,
                                        timeout: 750
                                    });

                                    //enable browser based spell checking
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
                                if (typeof tinyMceService.createLinkPicker !== "undefined") {
                                    tinyMceService.createLinkPicker(editor, $scope);
                                }

                                //Create the insert macro plugin
                                tinyMceService.createInsertMacro(editor, $scope);
                            };

                            /** Loads in the editor */
                            function loadTinyMce() {

                                //we need to add a timeout here, to force a redraw so TinyMCE can find
                                //the elements needed
                                $timeout(function () {
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

                });

            };

            return {
                restrict: "E",
                replace: true,
                template: "<div class='mortar-item--richtext'>" +
                    "<div class='mortar-button-bar mortar-button-bar--horizontal mortar-button-bar--tr'>" +
                    "<a href='#' ng-click=\"remove()\" prevent-default><i class='icon-delete' /></a>" +
                    "</div>" +
                    "<textarea ng-model=\"model.value\" rows=\"10\" id=\"mortar_rte_{{guid}}\" class=\"mortar_rte mortar_rte_{{guid}}\" name=\"m_{{guid}}\"></textarea>" +
                    "<div class='mce-hit-area' />" +
                    "</div>",
                scope: {
                    model: '=',
                    layoutConfig: '='
                },
                link: link
            };

        }
    ]);

angular.module("umbraco.directives").directive('mortarLinkItem',
    [
        "$compile",
        "$routeParams",
        "dialogService",
        "notificationsService",
        "entityResource",
        "Our.Umbraco.Mortar.Resources.mortarResources",
        function ($compile, $routeParams, dialogService, notificationsService, entityResource, mortarResources) {

            var link = function ($scope, element, attrs, ctrl) {

                // Setup model
                $scope.node = {
                    name: "..."
                };

                $scope.configure = function () {
                    dialogService.treePicker(dialogConfig);
                };

                $scope.remove = function () {
                    $scope.model = null;
                };

                var dialogConfig = {
                    entityType: "Document",
                    section: "content",
                    treeAlias: "content",
                    multiPicker: false,
                    callback: function (data) {
                        $scope.model.value = data.id;
                        $scope.node = data;
                    }
                };

                function init() {
                    if ($scope.model.value && $scope.model.value != "-1") {
                        // Grab the node from the id
                        entityResource.getById($scope.model.value, "Document").then(function (data) {
                            $scope.node = data;
                        });
                    } else if ($scope.model.value && $scope.model.value == "-1") {
                        // Set model value back to empty
                        $scope.model.value = "";
                        // Show the content dialog
                        $scope.configure();
                    }
                }

                if ($scope.layoutConfig && "pickerDtdId" in $scope.layoutConfig) {
                    mortarResources.getDataTypePreValues($scope.layoutConfig.pickerDtdId).then(function (pickerConfig) {
                        // We only realy want to know where the picker should start
                        if (pickerConfig.startNode.type == "content") {
                            if (pickerConfig.startNode.query) {
                                var rootId = $routeParams.id;
                                entityResource.getByQuery(pickerConfig.startNode.query, rootId, "Document").then(function (ent) {
                                    dialogConfig.startNodeId = ent.id;
                                    init();
                                });
                            } else {
                                dialogConfig.startNodeId = pickerConfig.startNode.id;
                                init();
                            }
                        } else {
                            init();
                        }
                    });
                } else {
                    init();
                }

            };

            return {
                restrict: "E",
                replace: true,
                template: "<div class='mortar-item--link mortar-item--vcenter'>" +
                    "<div class='mortar-button-bar mortar-button-bar--horizontal mortar-button-bar--tr'>" +
                    "<a href='#' ng-click=\"configure()\" prevent-default><i class='icon-link' /></a>" +
                    "<a href='#' ng-click=\"remove()\" prevent-default><i class='icon-delete' /></a>" +
                    "</div>" +
                    "<div class='mortar-item__label'>{{node.name}}</div>" +
                    "</div>",
                scope: {
                    model: '=',
                    layoutConfig: '='
                },
                link: link
            };

        }
    ]);

angular.module("umbraco.directives").directive('mortarMediaItem',
    [
        "$compile",
        "$routeParams",
        "dialogService",
        "notificationsService",
        "entityResource",
        "Our.Umbraco.Mortar.Resources.mortarResources",
        function ($compile, $routeParams, dialogService, notificationsService, entityResource, mortarResources) {

            var link = function ($scope, element, attrs, ctrl) {

                // Setup model
                $scope.node = {
                    name: "...",
                    url: ""
                };

                $scope.configure = function () {
                    dialogService.mediaPicker(dialogConfig);
                };

                $scope.remove = function () {
                    $scope.model = null;
                };

                var dialogConfig = {
                    onlyImages: true,
                    callback: function (data) {
                        $scope.model.value = data.id;
                        $scope.node.name = data.name;
                        $scope.node.url = data.image + "?width=500&height=220&mode=max";
                    }
                };

                function init() {
                    if ($scope.model.value && $scope.model.value != "-1") {
                        // Grab the node from the id
                        entityResource.getById($scope.model.value, "Media").then(function (data) {
                            $scope.node.name = data.name;

                            // Only set the URL if it's an image
                            if (typeof data.metaData.umbracoFile !== "undefined") {
                                $scope.node.url = data.metaData.umbracoFile.Value + "?width=500&height=220&mode=max";
                            }
                        });
                    } else if ($scope.model.value && $scope.model.value == "-1") {
                        // Set model value back to empty
                        $scope.model.value = "";
                        // Show the content dialog
                        $scope.configure();
                    }
                }

                init();

            };

            return {
                restrict: "E",
                replace: true,
                template: "<div class='mortar-item--media mortar-item--vcenter'>" +
                    "<div class='mortar-button-bar mortar-button-bar--horizontal mortar-button-bar--tr'>" +
                    "<a href='#' ng-click=\"configure()\" prevent-default><i class='icon-picture' /></a>" +
                    "<a href='#' ng-click=\"remove()\" prevent-default><i class='icon-delete' /></a>" +
                    "</div>" +
                    "<div class='mortar-item__label' ng-hide='node.url'>{{node.name}}</div>" +
                    "<img class='mortar-item__image' ng-show='node.url' ng-src='{{node.url}}' />" +
                    "</div>",
                scope: {
                    model: '=',
                    layoutConfig: '='
                },
                link: link
            };

        }
    ]);

angular.module("umbraco.directives").directive('mortarEmbedItem',
    [
        "$compile",
        "$routeParams",
        "dialogService",
        "notificationsService",
        "entityResource",
        "Our.Umbraco.Mortar.Resources.mortarResources",
        function ($compile, $routeParams, dialogService, notificationsService, entityResource, mortarResources) {

            var link = function ($scope, element, attrs, ctrl) {

                // Setup model
                $scope.configure = function () {
                    dialogService.embedDialog(dialogConfig);
                };

                $scope.remove = function () {
                    $scope.model = null;
                };

                var dialogConfig = {
                    callback: function (data) {
                        $scope.model.value = data;
                    }
                };

                if ($scope.model.value && $scope.model.value == "-1") {
                    // Set model value back to empty
                    $scope.model.value = "";
                    // Show the content dialog
                    $scope.configure();
                }

            };

            return {
                restrict: "E",
                replace: true,
                template: "<div class='mortar-item--embed mortar-item--vcenter'>" +
                    "<div class='mortar-button-bar mortar-button-bar--horizontal mortar-button-bar--tr'>" +
                    "<a href='#' ng-click=\"configure()\" prevent-default><i class='icon-display' /></a>" +
                    "<a href='#' ng-click=\"remove()\" prevent-default><i class='icon-delete' /></a>" +
                    "</div>" +
                    "<div class='mortar-item__label' ng-hide='model.value'>...</div>" +
                    "<div class=\"mortar-item__embed\" ng-show='model.value' ng-bind-html-unsafe=\"model.value\"></div>" +
                    "</div>",
                scope: {
                    model: '=',
                    layoutConfig: '='
                },
                link: link
            };

        }
    ]);

angular.module("umbraco.directives").directive('mortarDocTypeItem',
    [
        "Our.Umbraco.Mortar.Services.docTypeDialogService",
        function (docTypeDialogService) {

            var link = function ($scope, element, attrs, ctrl) {

                $scope.configure = function () {
                    docTypeDialogService.open({
                        allowedDocTypes: $scope.layoutConfig.allowedDocTypes,
                        dialogData: {
                            docType: $scope.model.docType,
                            value: $scope.model.value
                        },
                        callback: function (data) {
                            console.log(data);
                            $scope.model.docType = data.docType;
                            $scope.model.value = data.value;
                        }
                    });
                };

                $scope.remove = function () {
                    $scope.model = null;
                };

                if ($scope.model.value && $scope.model.value == "-1") {
                    // Set model value back to empty
                    $scope.model.value = "";
                    // Show the content dialog
                    $scope.configure();
                }

            };

            return {
                restrict: "E",
                replace: true,
                template: "<div class='mortar-item--link mortar-item--vcenter'>" +
                    "<div class='mortar-button-bar mortar-button-bar--horizontal mortar-button-bar--tr'>" +
                    "<a href='#' ng-click=\"configure()\" prevent-default><i class='icon-code' /></a>" +
                    "<a href='#' ng-click=\"remove()\" prevent-default><i class='icon-delete' /></a>" +
                    "</div>" +
                    "<div class='mortar-item__label'>{{model.value['name']}}</div>" +
                    "</div>",
                scope: {
                    model: '=',
                    layoutConfig: '='
                },
                link: link
            };

        }
    ]);

angular.module("umbraco.directives").directive('jsonTextarea', function () {
    return {
        restrict: 'A',
        require: 'ngModel',
        link: function (scope, element, attr, ctrl) {
            ctrl.$formatters.unshift(function (modelValue) {
                try {
                    return angular.toJson(modelValue, true);
                } catch (e) {
                    return modelValue;
                }
            });
            ctrl.$parsers.unshift(function (viewValue) {
                try {
                    return angular.fromJson(viewValue);
                } catch (e) {
                    return viewValue;
                }
            });
        }
    };
});