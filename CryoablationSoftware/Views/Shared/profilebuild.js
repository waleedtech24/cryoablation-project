// wwwroot/js/dashboard.js - Complete File
$(document).ready(function() {
    // Initialize dashboard
    initializeDashboard();
    
    // Auto-refresh dashboard data every 5 minutes
    setInterval(refreshDashboardData, 300000);
    
    // Initialize completion circle animation
    animateCompletionCircle();
    
    // Load recent activities
    loadRecentActivities();
    
    // Setup notification handlers
    setupNotificationHandlers();
    
    // Initialize charts if available
    initializeCharts();
    
    // Setup real-time updates
    setupRealTimeUpdates();
    
    // Track performance
    trackPagePerformance();
    
    // Setup keyboard shortcuts
    setupKeyboardShortcuts();
});

function initializeDashboard() {
    // Add fade-in animation to cards
    $('.stat-card, .content-card').each(function(index) {
        $(this).css('opacity', '0').delay(index * 100).animate({
            opacity: 1
        }, 500);
    });
    
    // Setup hover effects
    $('.stat-card').hover(
        function() {
            $(this).addClass('hover-effect');
        },
        function() {
            $(this).removeClass('hover-effect');
        }
    );
    
    // Setup quick action clicks
    $('.quick-action').click(function(e) {
        const action = $(this).data('action');
        handleQuickAction(action, e);
    });
    
    // Initialize tooltips
    $('[data-bs-toggle="tooltip"]').tooltip();
    
    // Setup card animations
    setupCardAnimations();
}

function setupCardAnimations() {
    // Intersection Observer for scroll animations
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    $(entry.target).addClass('fade-in-up');
                }
            });
        }, { threshold: 0.1 });
        
        document.querySelectorAll('.stat-card, .content-card').forEach(card => {
            observer.observe(card);
        });
    }
}

function animateCompletionCircle() {
    const circles = $('.completion-circle .circle-progress');
    
    circles.each(function() {
        const percentage = $(this).data('percentage') || 0;
        const circumference = 2 * Math.PI * 50; // radius = 50
        const offset = circumference - (percentage / 100) * circumference;
        
        $(this).css({
            'stroke-dasharray': circumference,
            'stroke-dashoffset': circumference
        });
        
        // Animate after a short delay
        setTimeout(() => {
            $(this).animate({
                'stroke-dashoffset': offset
            }, 1500, 'easeInOutQuart');
        }, 500);
    });
    
    // Animate percentage counter
    $('.completion-percentage').each(function() {
        const percentage = $(this).data('percentage') || 0;
        const element = $(this);
        
        $({ count: 0 }).animate({ count: percentage }, {
            duration: 1500,
            step: function() {
                element.text(Math.floor(this.count) + '%');
            },
            complete: function() {
                element.text(percentage + '%');
            }
        });
    });
}

function refreshDashboardData() {
    // Show loading indicator
    showLoadingIndicator();
    
    // Refresh stats
    $.get('/Dashboard/GetUpdatedStats')
        .done(function(data) {
            updateStatsCards(data);
            hideLoadingIndicator();
        })
        .fail(function() {
            console.log('Failed to refresh dashboard stats');
            hideLoadingIndicator();
            showNotificationToast('Failed to refresh data', 'error');
        });
    
    // Refresh activities
    loadRecentActivities();
}

function updateStatsCards(stats) {
    // Update each stat card with animation
    Object.keys(stats).forEach(key => {
        const card = $(`.stat-card[data-stat="${key}"]`);
        const valueElement = card.find('.stat-card-value');
        const currentValue = parseInt(valueElement.text()) || 0;
        const newValue = stats[key].value || 0;
        
        // Animate number change
        $({ count: currentValue }).animate({ count: newValue }, {
            duration: 1000,
            step: function() {
                valueElement.text(Math.floor(this.count));
            },
            complete: function() {
                valueElement.text(newValue);
            }
        });
        
        // Update change indicator
        const changeElement = card.find('.stat-card-change');
        if (stats[key].change !== undefined) {
            const change = stats[key].change;
            const changeText = change > 0 ? `+${change}%` : `${change}%`;
            const changeClass = change > 0 ? 'positive' : 'negative';
            
            changeElement.removeClass('positive negative')
                         .addClass(changeClass)
                         .html(`<i class="fas fa-arrow-${change > 0 ? 'up' : 'down'}"></i> ${changeText}`);
        }
    });
}

function loadRecentActivities() {
    const activitiesContainer = $('.activities-timeline');
    
    // Show loading
    activitiesContainer.html('<div class="text-center p-3"><i class="fas fa-spinner fa-spin"></i> Loading activities...</div>');
    
    $.get('/Dashboard/GetRecentActivities')
        .done(function(data) {
            updateActivitiesTimeline(data);
        })
        .fail(function() {
            activitiesContainer.html('<p class="text-danger text-center p-3">Failed to load activities</p>');
        });
}

function updateActivitiesTimeline(activities) {
    const timeline = $('.activities-timeline');
    timeline.empty();
    
    if (activities.length === 0) {
        timeline.html('<p class="text-muted text-center py-4">No recent activities</p>');
        return;
    }
    
    activities.forEach((activity, index) => {
        const activityItem = `
            <div class="activity-item" style="opacity: 0; transform: translateY(20px);">
                <div class="activity-icon">
                    <i class="fas fa-${getActivityIcon(activity.activityType)}"></i>
                </div>
                <div class="activity-content">
                    <div class="activity-text">${activity.description}</div>
                    <div class="activity-time">${formatActivityTime(activity.date)}</div>
                </div>
            </div>
        `;
        
        timeline.append(activityItem);
        
        // Animate each activity with delay
        setTimeout(() => {
            $('.activity-item').eq(index).animate({
                opacity: 1,
                transform: 'translateY(0)'
            }, 300);
        }, index * 100);
    });
}

function getActivityIcon(activityType) {
    const icons = {
        'profile_update': 'user-edit',
        'appointment': 'calendar-check',
        'document_upload': 'file-upload',
        'case_update': 'gavel',
        'payment': 'credit-card',
        'message': 'envelope',
        'login': 'sign-in-alt',
        'registration': 'user-plus',
        'verification': 'check-circle',
        'default': 'bell'
    };
    
    return icons[activityType] || icons.default;
}

function formatActivityTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffInMinutes = Math.floor((now - date) / (1000 * 60));
    
    if (diffInMinutes < 1) return 'Just now';
    if (diffInMinutes < 60) return `${diffInMinutes} minute${diffInMinutes > 1 ? 's' : ''} ago`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)} hour${Math.floor(diffInMinutes / 60) > 1 ? 's' : ''} ago`;
    if (diffInMinutes < 10080) return `${Math.floor(diffInMinutes / 1440)} day${Math.floor(diffInMinutes / 1440) > 1 ? 's' : ''} ago`;
    
    return date.toLocaleDateString('en-US', { 
        month: 'short', 
        day: 'numeric',
        year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    });
}

function handleQuickAction(action, event) {
    event.preventDefault();
    
    // Add loading effect to clicked action
    const actionElement = $(event.currentTarget);
    const originalContent = actionElement.html();
    actionElement.html('<i class="fas fa-spinner fa-spin"></i>');
    
    setTimeout(() => {
        actionElement.html(originalContent);
        
        switch(action) {
            case 'complete-profile':
                window.location.href = '/Profile/Build';
                break;
            case 'view-appointments':
                window.location.href = '/Appointments';
                break;
            case 'manage-cases':
                window.location.href = '/Cases';
                break;
            case 'view-earnings':
                showEarningsModal();
                break;
            case 'client-messages':
                window.location.href = '/Messages';
                break;
            case 'settings':
                window.location.href = '/Account/Settings';
                break;
            case 'new-appointment':
                window.location.href = '/Appointments/Create';
                break;
            case 'view-profile':
                window.location.href = '/Profile/View';
                break;
            default:
                console.log('Unknown action:', action);
        }
    }, 500);
}

function showEarningsModal() {
    // Create earnings modal if not exists
    if ($('#earningsModal').length === 0) {
        const modalHTML = `
            <div class="modal fade" id="earningsModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header bg-primary text-white">
                            <h5 class="modal-title">
                                <i class="fas fa-chart-line me-2"></i>
                                Earnings Overview
                            </h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="row mb-4">
                                <div class="col-md-4">
                                    <div class="earnings-stat text-center p-3 border rounded">
                                        <h3 class="text-success mb-2">Rs. 0</h3>
                                        <p class="text-muted mb-0">This Month</p>
                                        <small class="text-success">+0% from last month</small>
                                    </div>
                                </div>
                                <div class="col-md-4">
                                    <div class="earnings-stat text-center p-3 border rounded">
                                        <h3 class="text-info mb-2">Rs. 0</h3>
                                        <p class="text-muted mb-0">Last Month</p>
                                        <small class="text-muted">Previous period</small>
                                    </div>
                                </div>
                                <div class="col-md-4">
                                    <div class="earnings-stat text-center p-3 border rounded">
                                        <h3 class="text-warning mb-2">Rs. 0</h3>
                                        <p class="text-muted mb-0">Total Earnings</p>
                                        <small class="text-muted">All time</small>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="row">
                                <div class="col-12">
                                    <canvas id="earningsChart" height="200"></canvas>
                                </div>
                            </div>
                            
                            <hr>
                            <div class="d-flex justify-content-between align-items-center mb-3">
                                <h6 class="mb-0">Recent Transactions</h6>
                                <a href="/Payments" class="btn btn-sm btn-outline-primary">View All</a>
                            </div>
                            <div class="transactions-list">
                                <p class="text-muted text-center py-3">No transactions yet</p>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                            <a href="/Payments" class="btn btn-primary">Manage Payments</a>
                        </div>
                    </div>
                </div>
            </div>
        `;
        $('body').append(modalHTML);
    }
    
    $('#earningsModal').modal('show');
    
    // Initialize earnings chart
    initializeEarningsChart();
}

function setupNotificationHandlers() {
    // Mark notification as read
    $(document).on('click', '.notification-item', function() {
        const notificationId = $(this).data('notification-id');
        if (notificationId) {
            markNotificationAsRead(notificationId);
        }
    });
    
    // Notification dropdown toggle
    $('.notifications-toggle').click(function(e) {
        e.preventDefault();
        toggleNotificationsDropdown();
    });
    
    // Mark all notifications as read
    $('#markAllRead').click(function() {
        markAllNotificationsAsRead();
    });
    
    // Click outside to close notifications
    $(document).click(function(e) {
        if (!$(e.target).closest('.notifications-container').length) {
            $('.notifications-dropdown').removeClass('show');
        }
    });
}

function markNotificationAsRead(notificationId) {
    $.post('/Dashboard/MarkNotificationRead', { id: notificationId })
        .done(function(response) {
            if (response.success) {
                $(`.notification-item[data-notification-id="${notificationId}"]`)
                    .addClass('read')
                    .find('.notification-indicator')
                    .fadeOut();
                updateNotificationCount();
            }
        })
        .fail(function() {
            showNotificationToast('Failed to mark notification as read', 'error');
        });
}

function markAllNotificationsAsRead() {
    $.post('/Dashboard/MarkAllNotificationsRead')
        .done(function(response) {
            if (response.success) {
                $('.notification-item').addClass('read');
                $('.notification-indicator').fadeOut();
                updateNotificationCount();
                showNotificationToast('All notifications marked as read', 'success');
            }
        })
        .fail(function() {
            showNotificationToast('Failed to mark all notifications as read', 'error');
        });
}

function updateNotificationCount() {
    const unreadCount = $('.notification-item:not(.read)').length;
    const badge = $('.notification-badge');
    
    if (unreadCount > 0) {
        badge.text(unreadCount > 99 ? '99+' : unreadCount).show();
    } else {
        badge.hide();
    }
}

function toggleNotificationsDropdown() {
    const dropdown = $('.notifications-dropdown');
    dropdown.toggleClass('show');
    
    // Load notifications if dropdown is opened
    if (dropdown.hasClass('show')) {
        loadNotifications();
    }
}

function loadNotifications() {
    const container = $('.notifications-list');
    container.html('<div class="text-center p-3"><i class="fas fa-spinner fa-spin"></i> Loading...</div>');
    
    $.get('/Dashboard/GetNotifications')
        .done(function(notifications) {
            container.empty();
            
            if (notifications.length === 0) {
                container.html('<p class="text-muted text-center p-3">No notifications</p>');
                return;
            }
            
            notifications.forEach(notification => {
                const notificationHTML = `
                    <div class="notification-item ${notification.isRead ? 'read' : ''}" 
                         data-notification-id="${notification.id}">
                        <div class="notification-content">
                            <div class="notification-text">${notification.message}</div>
                            <div class="notification-time">${formatActivityTime(notification.createdAt)}</div>
                        </div>
                        ${!notification.isRead ? '<div class="notification-indicator"></div>' : ''}
                    </div>
                `;
                container.append(notificationHTML);
            });
            
            updateNotificationCount();
        })
        .fail(function() {
            container.html('<p class="text-danger text-center p-3">Failed to load notifications</p>');
        });
}

function showNotificationToast(message, type = 'info') {
    // Create toast container if not exists
    if ($('.toast-container').length === 0) {
        $('body').append('<div class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 9999;"></div>');
    }
    
    const iconClass = type === 'success' ? 'check-circle text-success' : 
                     type === 'error' ? 'exclamation-triangle text-danger' : 
                     'info-circle text-info';
    
    const toastHTML = `
        <div class="toast" role="alert" data-bs-autohide="true" data-bs-delay="5000">
            <div class="toast-header">
                <i class="fas fa-${iconClass} me-2"></i>
                <strong class="me-auto">OLA LAWS</strong>
                <small class="text-muted">now</small>
                <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    const toast = $(toastHTML);
    $('.toast-container').append(toast);
    
    // Show toast with Bootstrap
    if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
        const bsToast = new bootstrap.Toast(toast[0]);
        bsToast.show();
    } else {
        toast.show();
        setTimeout(() => toast.fadeOut(), 5000);
    }
    
    // Remove toast after it's hidden
    toast.on('hidden.bs.toast', function() {
        $(this).remove();
    });
}

function showLoadingIndicator() {
    if ($('.loading-overlay').length === 0) {
        const overlay = `
            <div class="loading-overlay position-fixed top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center" style="background: rgba(0,0,0,0.1); z-index: 9998;">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `;
        $('body').append(overlay);
    }
}

function hideLoadingIndicator() {
    $('.loading-overlay').fadeOut(300, function() {
        $(this).remove();
    });
}

// Real-time updates using SignalR (if available)
function setupRealTimeUpdates() {
    if (typeof signalR !== 'undefined') {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/dashboardHub")
            .build();
        
        connection.start().then(function() {
            console.log('Dashboard real-time connection established');
        }).catch(function(err) {
            console.error('Dashboard real-time connection failed:', err);
        });
        
        // Listen for real-time updates
        connection.on("StatsUpdated", function(stats) {
            updateStatsCards(stats);
        });
        
        connection.on("NewActivity", function(activity) {
            prependActivityToTimeline(activity);
        });
        
        connection.on("NewNotification", function(notification) {
            showNotificationToast(notification.message, 'info');
            updateNotificationCount();
        });
    }
}

function prependActivityToTimeline(activity) {
    const timeline = $('.activities-timeline');
    const activityItem = `
        <div class="activity-item new-activity" style="opacity: 0; transform: translateY(-20px);">
            <div class="activity-icon">
                <i class="fas fa-${getActivityIcon(activity.activityType)}"></i>
            </div>
            <div class="activity-content">
                <div class="activity-text">${activity.description}</div>
                <div class="activity-time">${formatActivityTime(activity.date)}</div>
            </div>
        </div>
    `;
    
    timeline.prepend(activityItem);
    
    // Animate new activity
    $('.new-activity').animate({
        opacity: 1,
        transform: 'translateY(0)'
    }, 300).removeClass('new-activity');
    
    // Remove old activities if too many
    const activities = $('.activity-item');
    if (activities.length > 10) {
        activities.slice(10).fadeOut(300, function() {
            $(this).remove();
        });
    }
}

// Chart initialization (if Chart.js is available)
function initializeCharts() {
    // Monthly earnings chart
    const earningsChartCanvas = $('#monthlyEarningsChart');
    if (earningsChartCanvas.length && typeof Chart !== 'undefined') {
        const ctx = earningsChartCanvas[0].getContext('2d');
        
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                datasets: [{
                    label: 'Earnings (PKR)',
                    data: [0, 0, 0, 0, 0, 0],
                    borderColor: '#D4AF37',
                    backgroundColor: 'rgba(212, 175, 55, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    }
}

function initializeEarningsChart() {
    const chartCanvas = $('#earningsChart');
    if (chartCanvas.length && typeof Chart !== 'undefined') {
        const ctx = chartCanvas[0].getContext('2d');
        
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Consultations', 'Document Review', 'Court Cases'],
                datasets: [{
                    data: [0, 0, 0],
                    backgroundColor: ['#D4AF37', '#f4e37a', '#b8941f'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }
}

function setupKeyboardShortcuts() {
    $(document).keydown(function(e) {
        // Ctrl/Cmd + R for refresh
        if ((e.ctrlKey || e.metaKey) && e.keyCode === 82) {
            e.preventDefault();
            refreshDashboardData();
        }
        
        // Escape to close modals/dropdowns
        if (e.keyCode === 27) {
            $('.modal').modal('hide');
            $('.notifications-dropdown').removeClass('show');
        }
        
        // Ctrl/Cmd + N for new appointment
        if ((e.ctrlKey || e.metaKey) && e.keyCode === 78) {
            e.preventDefault();
            window.location.href = '/Appointments/Create';
        }
    });
}

// Performance monitoring
function trackPagePerformance() {
    // Track page load time
    window.addEventListener('load', function() {
        const loadTime = performance.now();
        console.log(`Dashboard loaded in ${Math.round(loadTime)}ms`);
        
        // Send analytics if needed
        if (loadTime > 3000) {
            console.warn('Dashboard loading slowly');
        }
    });
}

// Export functions for global access
window.DashboardJS = {
    refreshData: refreshDashboardData,
    showEarnings: showEarningsModal,
    loadActivities: loadRecentActivities,
    markNotificationRead: markNotificationAsRead,
    showToast: showNotificationToast
};