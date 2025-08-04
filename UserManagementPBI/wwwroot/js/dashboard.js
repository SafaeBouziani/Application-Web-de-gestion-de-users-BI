let itemsPerPage = 20;
let currentPage = 1;
function renderPagination() {
    const container = document.getElementById('pagination-container');
    container.innerHTML = '';

    // Dynamically get total items and pages
    const totalItems = getVisibleRowCount();
    const totalPages = Math.max(1, Math.ceil(totalItems / itemsPerPage));

    // Adjust currentPage if out of bounds
    if (currentPage > totalPages) currentPage = totalPages;

    // Info
    const info = document.createElement('div');
    info.className = 'pagination-info';
    const startItem = totalItems === 0 ? 0 : (currentPage - 1) * itemsPerPage + 1;
    const endItem = Math.min(currentPage * itemsPerPage, totalItems);
    info.innerHTML = `<span>${startItem}-${endItem} of ${totalItems}</span>`;
    container.appendChild(info);

    // Buttons
    const buttons = document.createElement('div');
    buttons.className = 'pagination-buttons';

    // First/Prev
    buttons.innerHTML += `
        <button class="pagination-nav" ${currentPage === 1 ? 'disabled' : ''} data-page="first">
            <img src="https://cdn.builder.io/api/v1/image/assets/TEMP/bdb03789f49fc3b5fd7707f3127a0748495b1294?placeholderIfAbsent=true&apiKey=099995ce3fec4a6aab6339dc5406b76b" class="nav-icon">
        </button>
        <button class="pagination-nav" ${currentPage === 1 ? 'disabled' : ''} data-page="prev">
            <img src="https://cdn.builder.io/api/v1/image/assets/TEMP/ad1f654cbad2df56c9c8fa02c0cda6bee8a14e94?placeholderIfAbsent=true&apiKey=099995ce3fec4a6aab6339dc5406b76b" class="nav-icon">
        </button>
    `;

    // Page numbers
    const pageNumbers = document.createElement('div');
    pageNumbers.className = 'page-numbers';
    for (let i = 1; i <= totalPages; i++) {
        pageNumbers.innerHTML += `<button class="page-number${i === currentPage ? ' active' : ''}" data-page="${i}">${i}</button>`;
    }
    buttons.appendChild(pageNumbers);

    // Next/Last
    buttons.innerHTML += `
        <button class="pagination-nav" ${currentPage === totalPages ? 'disabled' : ''} data-page="next">
            <img src="https://cdn.builder.io/api/v1/image/assets/TEMP/f95407e1120ced3f6671339f79f4cb6ab53667b8?placeholderIfAbsent=true&apiKey=099995ce3fec4a6aab6339dc5406b76b" class="nav-icon">
        </button>
        <button class="pagination-nav" ${currentPage === totalPages ? 'disabled' : ''} data-page="last">
            <img src="https://cdn.builder.io/api/v1/image/assets/TEMP/962ee5bd05ea19c4f088f6faa5a72caf3d507703?placeholderIfAbsent=true&apiKey=099995ce3fec4a6aab6339dc5406b76b" class="nav-icon">
        </button>
    `;
    container.appendChild(buttons);

    // Add event listeners
    container.querySelectorAll('.pagination-nav, .page-number').forEach(btn => {
        btn.addEventListener('click', function () {
            const type = this.getAttribute('data-page');
            if (type === 'first') currentPage = 1;
            else if (type === 'prev') currentPage = Math.max(1, currentPage - 1);
            else if (type === 'next') currentPage = Math.min(totalPages, currentPage + 1);
            else if (type === 'last') currentPage = totalPages;
            else currentPage = parseInt(type);

            renderPagination();
            loadPageData(currentPage); // update table
        });
    });
}

//
document.addEventListener('DOMContentLoaded', function () {
    // Initialize all interactive components
    initializeNavigation();
    initializeSearch();
    initializeTable();
    initializeFilters();
    renderPagination();
});

function getVisibleRowCount() {
    return document.querySelectorAll('.table-row:not([style*="display: none"])').length;
}
// Navigation Menu Functionality
function initializeNavigation() {
    const menuItems = document.querySelectorAll('.menu-item');

    menuItems.forEach(item => {
        item.addEventListener('click', function () {
            // Remove active class from all items
            menuItems.forEach(menuItem => {
                menuItem.classList.remove('active');
            });

            // Add active class to clicked item
            this.classList.add('active');

            // Update page content based on selection
            const menuText = this.querySelector('span').textContent;
            updatePageContent(menuText);
        });
    });
}

// Search Functionality
function initializeSearch() {
    const searchInput = document.querySelector('.search-input');
    const searchClear = document.querySelector('.search-clear');
    const searchCursor = document.querySelector('.search-cursor');

    // Handle search input
    searchInput.addEventListener('input', function () {
        const searchTerm = this.value.toLowerCase();
        filterTableRows(searchTerm);

        // Show/hide clear button
        if (this.value.length > 0) {
            searchClear.style.opacity = '1';
        } else {
            searchClear.style.opacity = '0.5';
        }
    });

    // Handle search clear
    searchClear.addEventListener('click', function () {
        searchInput.value = '';
        filterTableRows('');
        searchInput.focus();
        this.style.opacity = '0.5';
    });

    // Handle search focus/blur for cursor animation
    searchInput.addEventListener('focus', function () {
        searchCursor.style.display = 'block';
    });

    searchInput.addEventListener('blur', function () {
        searchCursor.style.display = 'none';
    });
}

// Table Functionality
function initializeTable() {
    const selectAllCheckbox = document.getElementById('select-all');
    const rowCheckboxes = document.querySelectorAll('.table-checkbox:not(#select-all)');
    const tableRows = document.querySelectorAll('.table-row');

    // Handle select all functionality
    selectAllCheckbox.addEventListener('change', function () {
        const isChecked = this.checked;

        rowCheckboxes.forEach(checkbox => {
            checkbox.checked = isChecked;
            updateRowSelection(checkbox);
        });

        updateSelectedCount();
    });

    // Handle individual row selection
    rowCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            updateRowSelection(this);
            updateSelectAllState();
            updateSelectedCount();
        });
    });

    // Handle sortable columns
    const sortableHeaders = document.querySelectorAll('.sortable');
    sortableHeaders.forEach(header => {
        header.addEventListener('click', function () {
            sortTable(this);
        });
    });

    // Handle copy email functionality
    const copyIcons = document.querySelectorAll('.copy-icon');
    copyIcons.forEach(icon => {
        icon.addEventListener('click', function () {
            const emailText = this.parentElement.querySelector('span').textContent;
            copyToClipboard(emailText);
            showCopyFeedback(this);
        });
    });
}

// Pagination Functionality
function initializePagination() {
    const pageNumbers = document.querySelectorAll('.page-number');
    const paginationNavs = document.querySelectorAll('.pagination-nav');
    const paginationDropdown = document.querySelector('.pagination-dropdown');

    // Handle page number clicks
    pageNumbers.forEach(pageNumber => {
        pageNumber.addEventListener('click', function () {
            // Remove active class from all page numbers
            pageNumbers.forEach(page => page.classList.remove('active'));

            // Add active class to clicked page
            this.classList.add('active');

            // Update table content for selected page
            const pageNum = parseInt(this.textContent);
            loadPageData(pageNum);
        });
    });

    // Handle navigation buttons
    paginationNavs.forEach(nav => {
        nav.addEventListener('click', function () {
            const currentPage = document.querySelector('.page-number.active');
            const currentPageNum = parseInt(currentPage.textContent);
            const navIcon = this.querySelector('.nav-icon');
            const iconSrc = navIcon.src;

            let newPageNum = currentPageNum;

            // Determine navigation direction based on icon
            if (iconSrc.includes('https://cdn.builder.io/api/v1/image/assets/TEMP/bdb03789f49fc3b5fd7707f3127a0748495b1294?placeholderIfAbsent=true&apiKey=099995ce3fec4a6aab6339dc5406b76b')) { // First page
                newPageNum = 1;
            } else if (iconSrc.includes('https://cdn.builder.io/api/v1/image/assets/TEMP/ad1f654cbad2df56c9c8fa02c0cda6bee8a14e94?placeholderIfAbsent=true&apiKey=099995ce3fec4a6aab6339dc5406b76b')) { // Previous page
                newPageNum = Math.max(1, currentPageNum - 1);
            } else if (iconSrc.includes('https://cdn.builder.io/api/v1/image/assets/TEMP/f95407e1120ced3f6671339f79f4cb6ab53667b8?placeholderIfAbsent=true&apiKey=099995ce3fec4a6aab6339dc5406b76b')) { // Next page
                newPageNum = currentPageNum + 1;
            } else if (iconSrc.includes('https://cdn.builder.io/api/v1/image/assets/TEMP/962ee5bd05ea19c4f088f6faa5a72caf3d507703?placeholderIfAbsent=true&apiKey=099995ce3fec4a6aab6339dc5406b76b')) { // Last page
                newPageNum = 16; // Assuming 16 is the last page
            }

            // Update active page
            if (newPageNum !== currentPageNum) {
                const targetPage = Array.from(pageNumbers).find(page =>
                    parseInt(page.textContent) === newPageNum
                );

                if (targetPage) {
                    pageNumbers.forEach(page => page.classList.remove('active'));
                    targetPage.classList.add('active');
                    loadPageData(newPageNum);
                }
            }
        });
    });

    // Handle pagination dropdown
    paginationDropdown.addEventListener('click', function () {
        // Toggle dropdown menu (implementation would depend on specific requirements)
        console.log('Pagination dropdown clicked');
    });
}

// Filter Functionality
function initializeFilters() {
    const filterButtons = document.querySelectorAll('.filter-button');
    const actionButtons = document.querySelectorAll('.action-button');

    // Handle filter button clicks
    filterButtons.forEach(button => {
        button.addEventListener('click', function () {
            const buttonText = this.querySelector('span').textContent;

            if (buttonText === 'Actualiser') {
                refreshData();
            } else if (buttonText.includes('Selected')) {
                toggleSelectionMenu();
            } else if (buttonText === 'Filtrer par') {
                toggleFilterMenu();
            }
        });
    });

    // Handle action button clicks
    actionButtons.forEach(button => {
        button.addEventListener('click', function () {
            const buttonText = this.querySelector('span').textContent;

            if (buttonText === 'Nouveau') {
                openNewUserDialog();
            } else if (buttonText === 'Import/Export') {
                openImportExportDialog();
            }
        });
    });
}

// Helper Functions

function updateRowSelection(checkbox) {
    const row = checkbox.closest('.table-row');
    if (checkbox.checked) {
        row.classList.add('selected');
    } else {
        row.classList.remove('selected');
    }
}

function updateSelectAllState() {
    const selectAllCheckbox = document.getElementById('select-all');
    const rowCheckboxes = document.querySelectorAll('.table-checkbox:not(#select-all)');
    const checkedBoxes = document.querySelectorAll('.table-checkbox:not(#select-all):checked');

    if (checkedBoxes.length === 0) {
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = false;
    } else if (checkedBoxes.length === rowCheckboxes.length) {
        selectAllCheckbox.checked = true;
        selectAllCheckbox.indeterminate = false;
    } else {
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = true;
    }
}

function updateSelectedCount() {
    const checkedBoxes = document.querySelectorAll('.table-checkbox:not(#select-all):checked');
    const selectedButton = document.querySelector('.filter-button span');

    if (selectedButton && selectedButton.textContent.includes('Selected')) {
        selectedButton.textContent = `${checkedBoxes.length} Selected`;
    }
}

function filterTableRows(searchTerm) {
    const tableRows = document.querySelectorAll('.table-row');

    tableRows.forEach(row => {
        const rowText = row.textContent.toLowerCase();
        if (rowText.includes(searchTerm)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });

    updateResultsCount();
    renderPagination();
}

function updateResultsCount() {
    const visibleRows = document.querySelectorAll('.table-row:not([style*="display: none"])');
    const resultsCount = document.querySelector('.results-count');
    if (resultsCount) {
        resultsCount.textContent = `${visibleRows.length} Resultats`;
    }
}

function sortTable(header) {
    const table = header.closest('table');
    const tbody = table.querySelector('tbody');
    const rows = Array.from(tbody.querySelectorAll('tr'));
    const columnIndex = Array.from(header.parentElement.children).indexOf(header);

    // Determine sort direction
    const isAscending = !header.classList.contains('sort-desc');

    // Remove sort classes from all headers
    const allHeaders = table.querySelectorAll('.sortable');
    allHeaders.forEach(h => {
        h.classList.remove('sort-asc', 'sort-desc');
    });

    // Add sort class to current header
    header.classList.add(isAscending ? 'sort-asc' : 'sort-desc');

    // Sort rows
    rows.sort((a, b) => {
        const aValue = a.children[columnIndex].textContent.trim();
        const bValue = b.children[columnIndex].textContent.trim();

        // Handle numeric sorting for ID column
        if (columnIndex === 1) { // ID column
            return isAscending ?
                parseInt(aValue) - parseInt(bValue) :
                parseInt(bValue) - parseInt(aValue);
        }

        // Handle text sorting
        return isAscending ?
            aValue.localeCompare(bValue) :
            bValue.localeCompare(aValue);
    });

    // Reorder rows in DOM
    rows.forEach(row => tbody.appendChild(row));
}

function copyToClipboard(text) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(() => {
            console.log('Email copied to clipboard:', text);
        }).catch(err => {
            console.error('Failed to copy email:', err);
        });
    } else {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = text;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        console.log('Email copied to clipboard (fallback):', text);
    }
}

function showCopyFeedback(icon) {
    const originalSrc = icon.src;
    // You could change the icon to a checkmark temporarily
    setTimeout(() => {
        // Reset icon after feedback
        icon.src = originalSrc;
    }, 1000);
}

function updatePageContent(menuText) {
    const sectionTitle = document.querySelector('.section-title');

    switch (menuText) {
        case 'Tableau de bord':
            sectionTitle.textContent = 'Utilisateurs BI';
            break;
        case 'Historique':
            sectionTitle.textContent = 'Historique des actions';
            break;
        case 'Statistiques':
            sectionTitle.textContent = 'Statistiques d\'utilisation';
            break;
        default:
            sectionTitle.textContent = 'Utilisateurs BI';
    }
}

function loadPageData(pageNumber) {
    // Simulate loading new page data
    console.log(`Loading data for page ${pageNumber}`);

    // Update pagination info
    const paginationInfo = document.querySelector('.pagination-info span');
    const startItem = (pageNumber - 1) * 20 + 1;
    const endItem = Math.min(pageNumber * 20, 300);
    paginationInfo.textContent = `${startItem}-${endItem} of 300`;
}

function refreshData() {
    console.log('Refreshing data...');
    // Add loading state
    const refreshButton = document.querySelector('.filter-button.active');
    const originalText = refreshButton.querySelector('span').textContent;
    refreshButton.querySelector('span').textContent = 'Actualisation...';

    // Simulate API call
    setTimeout(() => {
        refreshButton.querySelector('span').textContent = originalText;
        console.log('Data refreshed');
    }, 1000);
}

function toggleSelectionMenu() {
    console.log('Toggle selection menu');
    // Implementation would show/hide selection options
}

function toggleFilterMenu() {
    console.log('Toggle filter menu');
    // Implementation would show/hide filter options
}

function openNewUserDialog() {
    console.log('Opening new user dialog');
    // Implementation would open a modal or navigate to new user form
}

function openImportExportDialog() {
    console.log('Opening import/export dialog');
    // Implementation would open import/export options
}

// Utility function to handle responsive behavior
function handleResize() {
    const sidebar = document.querySelector('.sidebar');
    const mainContent = document.querySelector('.main-content');

    if (window.innerWidth <= 768) {
        // Mobile layout adjustments
        sidebar.style.position = 'relative';
        mainContent.style.marginLeft = '0';
    } else {
        // Desktop layout
        sidebar.style.position = 'fixed';
        mainContent.style.marginLeft = '324px';
    }
}

// Add resize listener
window.addEventListener('resize', handleResize);

// Initialize responsive behavior
handleResize();

// Add smooth scrolling for better UX
document.documentElement.style.scrollBehavior = 'smooth';

// Add keyboard navigation support
document.addEventListener('keydown', function (e) {
    // Handle Escape key to close any open menus
    if (e.key === 'Escape') {
        // Close any open dropdowns or menus
        console.log('Escape pressed - closing menus');
    }

    // Handle Enter key on focused elements
    if (e.key === 'Enter' && document.activeElement.classList.contains('menu-item')) {
        document.activeElement.click();
    }
});

// Add focus management for accessibility
const focusableElements = document.querySelectorAll(
    'button, input, [tabindex]:not([tabindex="-1"])'
);

focusableElements.forEach(element => {
    element.addEventListener('focus', function () {
        this.style.outline = '2px solid #0ea5e9';
        this.style.outlineOffset = '2px';
    });

    element.addEventListener('blur', function () {
        this.style.outline = 'none';
    });
});


// Add tabindex to menu items for accessibility
document.querySelectorAll('.menu-item').forEach(item => {
    item.setAttribute('tabindex', '0');
});

// Modified Search Initialization
function initializeSearch() {
    const searchInput = document.querySelector('.search-input');
    const searchClear = document.querySelector('.search-clear');
    const searchCursor = document.querySelector('.search-cursor');

    searchInput.addEventListener('input', function () {
        const searchTerm = this.value.toLowerCase();
        filterTableRows(searchTerm);

        if (this.value.length > 0) {
            searchClear.classList.add('visible');
        } else {
            searchClear.classList.remove('visible');
        }
    });

    searchClear.addEventListener('click', function () {
        searchInput.value = '';
        filterTableRows('');
        searchInput.focus();
        this.classList.remove('visible');
    });

    searchInput.addEventListener('focus', function () {
        searchCursor.style.display = 'block';
    });

    searchInput.addEventListener('blur', function () {
        searchCursor.style.display = 'none';
    });
}

// Modify showCopyFeedback for icon opacity feedback
function showCopyFeedback(icon) {
    icon.style.opacity = '0.5';
    setTimeout(() => {
        icon.style.opacity = '1';
    }, 800);
}

// Fix sorting to avoid duplicated sort on icon clicks
document.querySelectorAll('.sortable').forEach(header => {
    header.addEventListener('click', function (e) {
        if (e.target.tagName.toLowerCase() === 'img') {
            e.stopPropagation();
        }
        sortTable(this);
    });
});


// === SIDEBAR TOGGLE ===
const sidebar = document.querySelector('.sidebar');
const toggleIcon = document.querySelector('.icon-r'); // This is the right arrow icon

if (toggleIcon) {
    toggleIcon.style.cursor = 'pointer';
    toggleIcon.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        document.querySelector('.main-content').classList.toggle('expanded');
    });
}


// === DYNAMIC SELECTED COUNT ===
function updateSelectedCount() {
    const checkedBoxes = document.querySelectorAll('.table-checkbox:not(#select-all):checked');
    const selectedButton = Array.from(document.querySelectorAll('.filter-button')).find(btn =>
        btn.textContent.includes('Selected')
    );

    if (selectedButton) {
        const span = selectedButton.querySelector('span');
        span.textContent = `${checkedBoxes.length} Selected`;
    }
}
