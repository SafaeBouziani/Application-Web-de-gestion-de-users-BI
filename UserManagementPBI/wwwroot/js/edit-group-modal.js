(function () {
    var state = {
        Title: $('#edit-group-form .group-title').val(),
        Comment: $('#edit-group-form .group-comment').val(),
        Reports: @Html.Raw(Json.Serialize(Model.Reports)),
        SelectedExistingUserIds: @Html.Raw(Json.Serialize(Model.SelectedExistingUserIds))
    };

    function renderReports() {
        var $list = $('#reports-list').empty();
        state.Reports.forEach(function (r, idx) {
            var $row = $(`
                <div class="report-row mb-1" data-index="${idx}">
                    <input class="form-control report-title mb-1" value="${r.Title}" placeholder="Titre du rapport"/>
                    <input class="form-control report-id-web mb-1" value="${r.IdWeb}" placeholder="Web ID"/>
                    <button type="button" class="btn btn-sm btn-danger btn-remove-report">Supprimer</button>
                </div>
            `);
            $list.append($row);

            $row.find('.report-title').on('input', function () { r.Title = $(this).val(); });
            $row.find('.report-id-web').on('input', function () { r.IdWeb = $(this).val(); });
            $row.find('.btn-remove-report').on('click', function () {
                state.Reports.splice(idx, 1);
                renderReports();
            });
        });
    }

    function initUsers() {
        var $sel = $('#user-select');
        Object.keys(window.__usersMap || {}).forEach(function (uid) {
            $sel.append(`<option value="${uid}">${window.__usersMap[uid]}</option>`);
        });

        if ($sel.data('choices')) $sel.data('choices').destroy();

        var choices = new Choices($sel[0], { removeItemButton: true, searchEnabled: true });
        choices.setValue(state.SelectedExistingUserIds.map(v => ({ value: v, label: window.__usersMap[v] })));

        $sel[0].addEventListener('change', function () {
            state.SelectedExistingUserIds = choices.getValue(true).map(Number);
        });
    }

    $('#btn-add-report').on('click', function () {
        state.Reports.push({ Id: 0, Title: '', IdWeb: '', Order: state.Reports.length + 1, Report: null });
        renderReports();
    });

    $('.btn-submit').on('click', function () {
        state.Title = $('#edit-group-form .group-title').val();
        state.Comment = $('#edit-group-form .group-comment').val();

        var payload = state;
        var token = $('#edit-group-form input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Reports/EditReportAjax',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            headers: { 'RequestVerificationToken': token },
            success: function (resp) {
                if (resp.success) {
                    $('#EditReportModal').modal('hide');
                    alert('Groupe modifié avec succès.');
                    location.reload();
                } else alert('Erreur: ' + resp.message);
            }
        });
    });

    $(document).on('shown.bs.modal', '#EditReportModal', function () {
        renderReports();
        initUsers();
    });
})();
