/* Directives */
angular.module("umbraco.directives").directive('mortarLayout',
    function($timeout, $compile, $routeParams, dialogService, notificationsService) {

        var link = function($scope, element, attrs, ctrl) {

            // Setup scope
            $scope.model = $scope.model || {};
            $scope.model.cellIds = [];
            $scope.model.value = $scope.model.value || {};

            //console.log($scope.model.value);

            $scope.removeSubLayout = function (cellId, index) {
                $scope.model.value[cellId].splice(index, 1);
            };

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
                    makeRowsSortable();

                } else {
                    
                    // Oops, too many items
                    notificationsService.error("Container already has the maximum number of items allowed.");
                }
            };

            $scope.hasItems = function(cellId) {
                return typeof $scope.model.value[cellId] !== "undefined"
                    && $scope.model.value[cellId].length > 0;
            };

            // Setup sorting
            var makeRowsSortable = function() {

                $("#mortar-" + $scope.model.id + " .mortar-sub-layouts.ui-sortable").sortable("destroy");
                $("#mortar-" + $scope.model.id + " .mortar-sub-layouts")
                    .sortable({
                        containment: "parent",
                        handle: ".mortar-sub-layout__sort",
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
                    .append("<div data-id='" + id + "' class='mortar-sub-layouts' ng-show=\"hasItems('" + id + "')\">" +
                        "<mortar-sub-layout ng-repeat=\"item in model.value['" + id + "']\" model=\"item\" cell-id=\"" + id + "\" />" +
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

angular.module("umbraco.directives").directive('mortarSubLayout',
    function($compile, $routeParams, dialogService, notificationsService, entityResource) {

        var link = function($scope, element, attrs, ctrl) {

            $scope.hasValue = function (cellIndex) {
                var value =  $scope.model.items != undefined &&
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

            var subLayout = $scope.model.layout.split(',');

            // Convert the template into an angular template
            var template = $("<div />");

            // Create the toolbar
            template.append($("<div class='mortar-sub-layout__button-bar mortar-button-bar mortar-button-bar--vertical mortar-button-bar--tl'>" +
                "<a href='#' ng-click=\"$parent.removeSubLayout(cellId, $parent.$index)\" prevent-default><i class='icon-delete' /></a>" +
                "<a href='#' class='mortar-sub-layout__sort' prevent-default><i class='icon-list' /></a>" +
                "</div>"));

            // Create the table
            var tbl = $("<table />");
            var tr = $("<tr />");
            for (var j = 0; j < subLayout.length; j++) {
                tr.append($("<td width='" + subLayout[j] + "%'>" +
                    "<div class='mortar-sub-layout__cell'>" +
                    "<div class='mortar-button-bar mortar-button-bar--horizontal mortar-button-bar--tr' ng-hide=\"hasValue(" + j + ")\">" +
                    "<a href='#' ng-click=\"setCellType('" + j + "','richtext')\" prevent-default><i class='icon-edit' /></a>" +
                    "<a href='#' ng-click=\"setCellType('" + j + "','link')\" prevent-default><i class='icon-link' /></a>" +
                    "<a href='#' ng-click=\"setCellType('" + j + "','doctype')\" prevent-default><i class='icon-settings' /></a>" +
                    "</div>" +
                    "<div class='mortar-sub-layout__cell-spacer' ng-hide=\"hasValue(" + j + ")\" />" +
                    "<mortar-item model='model.items[" + j + "]' />" +
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
            template: "<div class='mortar-sub-layouts__item mortar-sub-layout' />",
            link: link,
            scope: {
                model: '=',
                cellId: '@',
            }
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

                // Add new item
                if (newValue !== undefined && newValue !== null) {
                    var el = $compile("<mortar-" + newValue.type + "-item model='model' />")($scope);
                    element.append(el);
                }

            });

        };

        return {
            restrict: "E",
            replace: true,
            template: "<div class='mortar-item' />",
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
            $scope.node = {
                name: "..."
            };

            $scope.configure = function () {
                dialogService.contentPicker({
                    callback: function (data) {
                        $scope.model.value = data.id;
                        $scope.node = data;
                    }
                });
            };

            $scope.remove = function() {
                $scope.model = null;
            };
            
            if ($scope.model.value && $scope.model.value != "-1") {
                // Grab the node from the id
                entityResource.getById($scope.model.value, "Document").then(function(data) {
                    $scope.node = data;
                });
            } else if ($scope.model.value && $scope.model.value == "-1") {
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
                "<a href='#' ng-click=\"configure()\" prevent-default><i class='icon-link' /></a>" +
                "<a href='#' ng-click=\"remove()\" prevent-default><i class='icon-delete' /></a>" +
                "</div>" +
                "<div class='mortar-item__label'>{{node.name}}</div>" +
                "</div>",
            scope: {
                model: '=' 
            },
            link: link
        };

    });

angular.module("umbraco.directives").directive('mortarRichtextItem',
    function ($q, $timeout, tinyMceService, assetsService, angularHelper, stylesheetResource) {

        var guid = function() {
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

            if ($scope.model.value && $scope.model.value == "-1") {
                // We don't need to initialize anything as a result of a click,
                // so just set the value to empty
                $scope.model.value = "";
            };

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

                var editorConfig = tinyMceService.defaultPrevalues();

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
                angular.forEach(editorConfig.stylesheets, function(val, key) {
                    stylesheets.push("../css/" + val + ".css?" + new Date().getTime());

                    await.push(stylesheetResource.getRulesByName(val).then(function(rules) {
                        angular.forEach(rules, function(rule) {
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
                        theme: "modern",
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
                        theme_modern_toolbar_location: "bottom",
                    };


                    if (tinyMceConfig.customConfig) {
                        angular.extend(baseLineConfigObj, tinyMceConfig.customConfig);
                    }

                    // Remove some elements from the toobar
                    baseLineConfigObj.toolbar = baseLineConfigObj.toolbar.replace("outdent | indent |", "");

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
                                over: function() {
                                    $(this).find(".mce-toolbar").slideDown("fast");
                                },
                                out: function() {
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
                        tinyMceService.createLinkPicker(editor, $scope);

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
                model: '='
            },
            link: link
        };

    });

angular.module("umbraco.directives").directive('mortarDoctypeItem',
    function ($compile, $routeParams, dialogService, editorState) {

        var link = function ($scope, element, attrs, ctrl) {

            $scope.configure = function ()
            {
                var currentEditorState = editorState.current;
                var callback = function () {
                    // We create a new editor state in the dialog,
                    // so be sure to set the previous one back 
                    // when we are done.
                    editorState.set(currentEditorState);
                };

                dialogService.open({
                    template: "/App_Plugins/Mortar/Views/mortar.docTypeDialog.html",
                    scope: $scope,
                    show: true,
                    callback: callback,
                    closeCallback: callback
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
                "<a href='#' ng-click=\"configure()\" prevent-default><i class='icon-settings' /></a>" +
                "<a href='#' ng-click=\"remove()\" prevent-default><i class='icon-delete' /></a>" +
                "</div>" +
                "<div class='mortar-item__label'>{{model.docType}}</div>" +
                "</div>",
            scope: {
                model: '='
            },
            link: link
        };

    });
