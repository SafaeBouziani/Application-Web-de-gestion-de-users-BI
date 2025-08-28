// wwwroot/js/users-modal.js
// Requires jQuery and Bootstrap JS (your _Layout has those)
(function () {
    // Global namespace
    var CreateUserModal = {
        state: {
            step: 1,
            stepCount: 3,
            newGroups: []
        },

        init: function () {
            var self = this;
            // wire nav buttons
            var $modal = $('#CreateGroupModal');


            $modal.on('click', '.btn-next', function () { self.next(); });
            $modal.on('click', '.btn-prev', function () { self.prev(); });
            $modal.on('click', '.btn-submit', function () { self.submit(); });
            $modal.on('click', '.btn-close-modal', function () { $modal.modal('hide'); });

            $modal.on('click', '#btn-add-quick-group', function () {
                var title = $('#quick-new-group-title').val();
                if (!title || !title.trim()) { alert('Entrez un titre'); return; }
                self.state.newGroups.push({ title: title.trim(), comment: '', reports: [], selectedExistingUserIds: [] });
                $('#quick-new-group-title').val('');
                self.renderQuickNewGroupsList();
                self.renderNewGroupsPanels();
            });

            // add group in step3
            $modal.on('click', '#btn-add-group', function () {
                self.state.newGroups.push({ title: '', comment: '', reports: [], selectedExistingUserIds: [] });
                self.renderNewGroupsPanels();
            });

            // remove quick new group list item
            $modal.on('click', '.quick-group-remove', function () {
                var idx = $(this).data('index');
                self.state.newGroups.splice(idx, 1);
                self.renderQuickNewGroupsList();
                self.renderNewGroupsPanels();
            });

            // dynamic group fields change (delegated)
            $modal.on('input', '.group-title', function () {
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                self.state.newGroups[gidx].title = $(this).val();
                self.renderQuickNewGroupsList();
            });
            $modal.on('input', '.group-comment', function () {
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                self.state.newGroups[gidx].comment = $(this).val();
            });

            // add report inside a group
            $modal.on('click', '.btn-add-report', function () {
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                var group = self.state.newGroups[gidx];
                group.reports.push({ Title: '', Id: '', Order: (group.reports.length + 1), IdWeb: '', Report: '' });
                self.renderGroupReports(gidx);
            });

            // remove report
            $modal.on('click', '.btn-remove-report', function () {
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                var ridx = $(this).closest('.report-row').data('reportIndex');
                self.state.newGroups[gidx].reports.splice(ridx, 1);
                self.renderGroupReports(gidx);
            });

            // dynamic report inputs -> update state
            $modal.on('input', '.report-title', function () {
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                var ridx = $(this).closest('.report-row').data('reportIndex');
                self.state.newGroups[gidx].reports[ridx].Title = $(this).val();
            });
            $modal.on('input', '.report-order', function () {
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                var ridx = $(this).closest('.report-row').data('reportIndex');
                self.state.newGroups[gidx].reports[ridx].Order = parseInt($(this).val() || '0');
            });
            $modal.on('change', '.report-id-bi', function () {
                var $row = $(this).closest('.report-row');
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                var ridx = $row.data('reportIndex');
                var selectedId = $(this).val();

                if (window.__catalogMap && window.__catalogMap[selectedId]) {
                    var reportInfo = window.__catalogMap[selectedId];
                    $row.find('.report-title').val(reportInfo.Name);
                    $row.find('.report-path').text("Path: /" + reportInfo.Path).show();

                    self.state.newGroups[gidx].reports[ridx].Title = reportInfo.Name;
                } else {
                    $row.find('.report-title').val('');
                    $row.find('.report-path').hide();

                    self.state.newGroups[gidx].reports[ridx].Title = '';
                }

                self.state.newGroups[gidx].reports[ridx].Id = selectedId;
            });

            // id_web input
            $modal.on('input', '.report-id-web', function () {
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                var ridx = $(this).closest('.report-row').data('reportIndex');
                self.state.newGroups[gidx].reports[ridx].IdWeb = $(this).val();
            });

            // report file upload
            $modal.on('input', '.report-file', function () {
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                var ridx = $(this).closest('.report-row').data('reportIndex');
                // we store the file object in state (server will handle binding)
                self.state.newGroups[gidx].reports[ridx].Report = this.files[0];
            });

            // remove group
            $modal.on('click', '.btn-remove-group', function () {
                if (!confirm('Remove this new group?')) return;
                var gidx = $(this).closest('.group-panel').data('groupIndex');
                self.state.newGroups.splice(gidx, 1);
                self.renderNewGroupsPanels();
            });



            // keep step label updated
            self.updateStepUI();
        },

        // UI helpers
        showStep: function (n) {
            var $modal = $('#CreateGroupModal');
            $modal.find('.step-pane').addClass('d-none');
            $modal.find('.step-pane[data-step="' + n + '"]').removeClass('d-none');

            $modal.find('.btn-prev').toggleClass('d-none', n === 1);
            $modal.find('.btn-next').toggleClass('d-none', n === this.state.stepCount);
            $modal.find('.btn-submit').toggleClass('d-none', n !== this.state.stepCount);
            $('#current-step-label').text(n);
        },

        updateStepUI: function () {
            this.showStep(this.state.step);
        },

        next: function () {

            if (this.state.step < this.state.stepCount) {
                this.state.step++;
                this.updateStepUI();
                if (this.state.step === 2) this.renderNewGroupsPanels();
                if (this.state.step === 3) this.renderSummary();
            }
        },

        prev: function () {
            if (this.state.step > 1) {
                this.state.step--;
                this.updateStepUI();
            }
        },

        // render the quick list in step 2 (from newGroups)
        renderQuickNewGroupsList: function () {
            var $list = $('#quick-new-groups-list').empty();
            if (!this.state.newGroups.length) {
                $list.text('(No quick groups added)');
                return;
            }
            this.state.newGroups.forEach(function (g, idx) {
                var $el = $('<div class="d-flex align-items-center mb-1"></div>');
                $el.append('<div style="flex:1"><strong>' + (g.title || '(untitled)') + '</strong></div>');
                $el.append('<button class="btn btn-sm btn-link quick-group-remove" data-index="' + idx + '">Remove</button>');
                $list.append($el);
            });
        },

        renderNewGroupsPanels: function () {
            var container = $('#new-groups-container').empty();
            var self = this;

            if (!this.state.newGroups.length) {
                container.append('<div class="text-muted">No new groups added yet. Use "Add new group" or the quick-add in Step 1.</div>');
                return;
            }

            this.state.newGroups.forEach(function (g, gidx) {
                var $panel = $(`
            <div class="group-panel" data-group-index="${gidx}">
                <div class="d-flex justify-content-between align-items-start mb-2">
                    <div style="flex:1">
                        <label>Titre du Groupe</label>
                        <input class="form-control group-title" placeholder="Group title" value="${g.title || ''}"/>
                    </div>
                    <div style="width:120px; margin-left:8px">
                        <label>&nbsp;</label><br/>
                        <button class="btn btn-sm btn-danger btn-remove-group">Supprimer le groupe</button>
                    </div>
                </div>
                <div class="form-group">
                    <label>Commentaire</label>
                    <textarea class="form-control group-comment" rows="2">${g.comment || ''}</textarea>
                </div>
                <div class="form-group">
                    <label>Rapports</label>
                    <div class="reports-list"></div>
                    <div class="mt-2"><button type="button" class="btn btn-sm btn-primary btn-add-report">Ajouter un rapport</button></div>
                </div>
                <div class="form-group">
                    <label>Assign Users</label>
                    <select class="user-select" multiple></select>
                </div>
            </div>
        `);

                container.append($panel);

                // populate users in the <select>
                var $userSelect = $panel.find('.user-select');
                Object.keys(window.__usersMap || {}).forEach(function (uid) {
                    $userSelect.append(`<option value="${uid}">${window.__usersMap[uid]}</option>`);
                });

                // destroy existing Choices instance if any
                if ($userSelect.data('choices')) $userSelect.data('choices').destroy();

                // initialize Choices.js
                var choices = new Choices($userSelect[0], {
                    removeItemButton: true,
                    searchEnabled: true,
                    placeholder: true,
                    placeholderValue: 'Select users',
                    shouldSort: false
                });

                // restore selected users
                if (g.selectedExistingUserIds && g.selectedExistingUserIds.length) {
                    choices.setValue(
                        g.selectedExistingUserIds.map(String).map(v => ({ value: v, label: window.__usersMap[v] }))
                    );
                }

                // track selection in state
                $userSelect[0].addEventListener('change', function () {
                    var idx = $userSelect.closest('.group-panel').data('groupIndex');
                    self.state.newGroups[idx].selectedExistingUserIds = choices.getValue(true).map(Number);
                });

                // render reports for this group
                self.renderGroupReports(gidx);
            });

            this.renderQuickNewGroupsList();
        },



        renderGroupReports: function (gidx) {
            var g = this.state.newGroups[gidx];
            var $panel = $('#new-groups-container').find('.group-panel[data-group-index="' + gidx + '"]');
            var $list = $panel.find('.reports-list').empty();

            if (!g.reports || !g.reports.length) {
                $list.append('<div class="text-muted">No reports yet</div>');
                return;
            }


            g.reports.forEach(function (r, ridx) {
                var pathText = '';
                var pathDisplay = 'none';
                if (window.__catalogMap && r.Id && window.__catalogMap[r.Id]) {
                    pathText = "Path: /" + window.__catalogMap[r.Id].Path;
                    pathDisplay = 'block';
                }
                var $row = $(`
                    <div class="report-row" data-report-index="${ridx}">
                        <select class="form-control report-id-bi" style="width:200px">
                            <option value="">-- Select a Report --</option>
                            ${Object.keys(window.__catalogMap || {}).map(function (key) {
                    var selected = (r.Id === key) ? 'selected' : '';
                    return `<option value="${key}" ${selected}>${window.__catalogMap[key].Name}</option>`;
                }).join('')}     
                        </select>
                        <input class="form-control report-title" placeholder="Report title" value="${r.Title || ''}"/>
                         <!-- File upload -->
                        <input class="form-control report-file" placeholder="Report" value="${r.Report || ''}"/>

                        <input class="form-control report-id-web" placeholder="Web ID" value="${r.IdWeb || ''}" />

                        <input class="form-control report-order" placeholder="Order" value="${r.Order || (ridx + 1)}" style="width:80px"/>

                        <button class="btn btn-sm btn-outline-danger btn-remove-report" type="button">Remove</button>
                    </div>
                    <div class="report-path text-muted" style="display:${pathDisplay}; font-size:0.9em; margin-left:5px;">
                        ${pathText}
                    </div>
                `);
                $list.append($row);
                $row.find('.report-id-bi').trigger('change');
            });

        },

        renderSummary: function () {
            var $c = $('#summary-content').empty();
            var s = this.state;

            // new groups & reports
            var $ng = $('<div class="mt-2"><strong>Nouveaux groupes</strong></div>');
            if (s.newGroups.length) {
                s.newGroups.forEach(function (g, gi) {
                    var $gdiv = $('<div style="padding:6px;border:1px solid #eee;margin-bottom:6px;border-radius:4px"></div>');
                    $gdiv.append('<div><strong>' + (g.title || '(untitled)') + '</strong></div>');
                    $gdiv.append('<div>' + (g.comment || '') + '</div>');

                    // reports
                    if (g.reports && g.reports.length) {
                        var $rul = $('<ul></ul>');
                        g.reports.forEach(function (r) {
                            $rul.append('<li>' + (r.Title || '(untitled)') +
                                ' — id:' + (r.Id || '') +
                                ' — order:' + (r.Order || '') + '</li>');
                        });
                        $gdiv.append('<div><em>Reports:</em></div>');
                        $gdiv.append($rul);
                    } else {
                        $gdiv.append('<div class="text-muted">No reports</div>');
                    }

                    // users
                    if (g.selectedExistingUserIds && g.selectedExistingUserIds.length) {
                        var $uul = $('<ul></ul>');
                        g.selectedExistingUserIds.forEach(function (uid) {
                            var name = window.__usersMap[uid] || '(unknown)';
                            $uul.append('<li>' + name + ' — id:' + uid + '</li>');
                        });
                        $gdiv.append('<div><em>Users:</em></div>');
                        $gdiv.append($uul);
                    } else {
                        $gdiv.append('<div class="text-muted">No users</div>');
                    }



                    $ng.append($gdiv);
                });
            } else {
                $ng.append('<div class="text-muted">No new groups</div>');
            }

            $c.append($ng);
        },


        submit: function () {
            var self = this;
            var groupId = parseInt($('#create-group-form input[name="Id"]').val() || "0");

            // construct payload by reading latest DOM state (step1 and newGroups)
            var payload = {
                NewGroups: []
            };

            // ensure newGroups reflect DOM (collect titles/comments/reports)
            $('#new-groups-container .group-panel').each(function () {
                var gidx = $(this).data('groupIndex');
                var gState = self.state.newGroups[gidx] || { title: '', comment: '', reports: [], selectedExistingUserIds : [] };
                // update from inputs just to be safe
                gState.title = $(this).find('.group-title').val();
                gState.comment = $(this).find('.group-comment').val();

                var reports = [];
                $(this).find('.report-row').each(function () {
                    var ridx = $(this).data('reportIndex');
                    var rTitle = $(this).find('.report-title').val();
                    var rId = $(this).find('.report-id-bi').val();
                    var rOrder = parseInt($(this).find('.report-order').val() || '0');
                    var rIdWeb = $(this).find('.report-id-web').val();
                    var rReport = $(this).find('.report-file').val(); // file input
                    reports.push({ Title: rTitle, Id: rId, Order: rOrder, IdWeb: rIdWeb, Report: rReport });
                });
                gState.reports = reports;
                gState.selectedExistingUserIds = self.state.newGroups[gidx].selectedExistingUserIds || [];

                payload.NewGroups.push(gState);
            });

            // basic validation


            // anti-forgery token
            var token = $('#create-group-form input[name="__RequestVerificationToken"]').val();
            var url = '/Reports/CreateReportAjax';     // Edit

            $.ajax({
                url: url,
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(payload),
                headers: {
                    'RequestVerificationToken': token
                },
                success: function (resp) {
                    if (resp && resp.success) {
                        // close modal and optionally reload
                        $('#CreateGroupModal').modal('hide');
                            alert('Groupe créé avec succès (id: ' + resp.groupId + '). La page va se raffraichir.');
                        location.reload();
                    } else {
                        alert('Server error: ' + (resp && resp.message ? resp.message : 'unknown'));
                    }
                },
                error: function (xhr) {
                    var info = 'An error occurred';
                    try { info = JSON.parse(xhr.responseText).message || xhr.responseText; } catch (e) { info = xhr.responseText; }
                    alert('Save failed: ' + info);
                }
            });
        }
    };



    // When partial is appended to body we initialize
    $(document).on('shown.bs.modal', '#CreateGroupModal', function () {
        CreateUserModal.init();
    });

    // Also initialize immediately if the modal was already shown
    $(document).ready(function () {
        if ($('#CreateGroupModal').length) {
            CreateUserModal.init();
        }
    });


})();
