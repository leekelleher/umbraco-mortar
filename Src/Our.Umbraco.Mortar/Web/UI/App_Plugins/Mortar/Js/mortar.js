/* Directives */
angular.module("umbraco.directives").directive('mortarLayout',
    function($compile, $routeParams, dialogService, notificationsService, entityResource) {

        var link = function($scope, element, attrs, ctrl) {

            // Setup scope
            $scope.model = $scope.model || {};
            $scope.model.cellIds = [];
            $scope.model.value = $scope.model.value || {};

            $scope.addSubLayout = function(cellId, layout) {

                // Make sure we can add
                var td = $("#wg-" + $scope.id + " td#" + cellId);
                if (typeof td.attr("data-max") === "undefined"
                    || typeof $scope.model.value[cellId] === "undefined"
                    || td.data("max") > $scope.model.value[cellId].length) {

                    // Ok, go ahead
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

            $scope.config = $scope.row;

            console.log($scope.config);

            var subLayout = $scope.config.layout.split(',');

            // Convert the template into an angular template
            var template = $("<div />");

            var tbl = $("<table class='sub-layout' />");
            var tr = $("<tr />");
            for (var j = 0; j < subLayout.length; j++) {
                tr.append($("<td width='" + subLayout[j] + "%'>" +
                    "<div class='brick-select'>" +
                    "<a href='#' ng-click=\"\" prevent-default><i class='icon-edit' /></a>" +
                    "<a href='#' ng-click=\"\" prevent-default><i class='icon-link' /></a>" +
                    "<a href='#' ng-click=\"\" prevent-default><i class='icon-settings' /></a>" +
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
            rep1ace: true,
            link: link
        };

    });