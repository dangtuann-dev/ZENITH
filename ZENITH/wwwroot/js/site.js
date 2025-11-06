const $ = document.querySelector.bind(document);
const $$ = document.querySelectorAll.bind(document);

/**
 * Hàm tải template
 *
 * Cách dùng:
 * <div id="parent"></div>
 * <script>
 *  load("#parent", "./path-to-template.html");
 * </script>
 */
function load(selector, path) {
    const cached = localStorage.getItem(path);
    if (cached) {
        $(selector).innerHTML = cached;
    }

    fetch(path)
        .then((res) => res.text())
        .then((html) => {
            if (html !== cached) {
                $(selector).innerHTML = html;
                localStorage.setItem(path, html);
            }
        })
        .finally(() => {
            window.dispatchEvent(new Event("template-loaded"));
        });
}

/**
 * Hàm kiểm tra một phần tử
 * có bị ẩn bởi display: none không
 */
function isHidden(element) {
    if (!element) return true;

    if (window.getComputedStyle(element).display === "none") {
        return true;
    }

    let parent = element.parentElement;
    while (parent) {
        if (window.getComputedStyle(parent).display === "none") {
            return true;
        }
        parent = parent.parentElement;
    }

    return false;
}

/**
 * Hàm buộc một hành động phải đợi
 * sau một khoảng thời gian mới được thực thi
 */
function debounce(func, timeout = 300) {
    let timer;
    return (...args) => {
        clearTimeout(timer);
        timer = setTimeout(() => {
            func.apply(this, args);
        }, timeout);
    };
}

/**
 * Hàm tính toán vị trí arrow cho dropdown
 *
 * Cách dùng:
 * 1. Thêm class "js-dropdown-list" vào thẻ ul cấp 1
 * 2. CSS "left" cho arrow qua biến "--arrow-left-pos"
 */
const calArrowPos = debounce(() => {
    if (isHidden($(".js-dropdown-list"))) return;

    const items = $$(".js-dropdown-list > li");

    items.forEach((item) => {
        const arrowPos = item.offsetLeft + item.offsetWidth / 2;
        item.style.setProperty("--arrow-left-pos", `${arrowPos}px`);
    });
});

// Tính toán lại vị trí arrow khi resize trình duyệt
window.addEventListener("resize", calArrowPos);

// Tính toán lại vị trí arrow sau khi tải template
window.addEventListener("template-loaded", calArrowPos);

/**
 * Giữ active menu khi hover
 *
 * Cách dùng:
 * 1. Thêm class "js-menu-list" vào thẻ ul menu chính
 * 2. Thêm class "js-dropdown" vào class "dropdown" hiện tại
 *  nếu muốn reset lại item active khi ẩn menu
 */
window.addEventListener("template-loaded", handleActiveMenu);

function handleActiveMenu() {
    const dropdowns = $$(".js-dropdown");
    const menus = $$(".js-menu-list");
    const activeClass = "menu-column__item--active";
    const removeActive = (menu) => {
        const activeItems = menu.querySelectorAll(`.${activeClass}`);
        activeItems.forEach((item) => item.classList.remove(activeClass));
    };

    // Khởi tạo menu
    const init = () => {
        // Xóa tất cả active trước
        removeAllActive();

        // Thêm active cho phần tử đầu tiên của menu chính
        const mainMenu = document.querySelector(".navbar__list .js-menu-list");
        if (mainMenu && mainMenu.children.length > 0 && window.innerWidth > 991) {
            mainMenu.children[0].classList.add(activeClass);
        }

        // Thêm sự kiện cho tất cả các menu item
        menus.forEach((menu) => {
            const items = menu.children;
            if (!items.length) return;

            Array.from(items).forEach((item) => {
                item.onmouseenter = () => {
                    if (window.innerWidth <= 991) return;
                    removeActive(menu);
                    item.classList.add(activeClass);
                };
                item.onclick = () => {
                    if (window.innerWidth > 991) return;
                    removeActive(menu);
                    item.classList.add(activeClass);
                    item.scrollIntoView();
                };
            });
        });
    };

    // Khởi tạo menu
    init();

    // Thêm sự kiện cho dropdown
    //dropdowns.forEach((dropdown) => {
    //    dropdown.onmouseenter = () => {
    //        if (window.innerWidth <= 991) return;

    //        // Xóa tất cả active
    //        removeAllActive();

    //        // Thêm active cho dropdown hiện tại
    //        const currentMenu = dropdown.querySelector(".js-menu-list");
    //        if (currentMenu && currentMenu.children.length > 0) {
    //            currentMenu.children[0].classList.add(activeClass);
    //        }
    //    };

    //    dropdown.onmouseleave = () => {
    //        if (window.innerWidth <= 991) return;
    //        init();
    //    };
    //});
    dropdowns.forEach((dropdown) => {
        dropdown.onmouseleave = () => init();
    });
}

/**
 * JS toggle
 *
 * Cách dùng:
 * <button class="js-toggle" toggle-target="#box">Click</button>
 * <div id="box">Content show/hide</div>
 */
window.addEventListener("template-loaded", initJsToggle);

function initJsToggle() {
    $$(".js-toggle").forEach((button) => {
        const target = button.getAttribute("toggle-target");
        if (!target) {
            document.body.innerText = `Cần thêm toggle-target cho: ${button.outerHTML}`;
        }
        button.onclick = (e) => {
            e.preventDefault();

            if (!$(target)) {
                return (document.body.innerText = `Không tìm thấy phần tử "${target}"`);
            }
            const isHidden = $(target).classList.contains("hide");

            requestAnimationFrame(() => {
                $(target).classList.toggle("hide", !isHidden);
                $(target).classList.toggle("show", isHidden);
            });
        };
        document.onclick = function (e) {
            if (!e.target.closest(target)) {
                const isHidden = $(target).classList.contains("hide");
                if (!isHidden) {
                    button.click();
                }
            }
        };
    });
}

window.addEventListener("template-loaded", () => {
    const links = $$(".js-dropdown-list > li > a");

    links.forEach((link) => {
        link.onclick = () => {
            if (window.innerWidth > 991) return;
            const item = link.closest("li");
            item.classList.toggle("navbar__item--active");
        };
    });
});

// Bật/tắt overlay mờ nền khi submenu mở
function setupNavbarOverlay() {
    const overlay = document.getElementById("menu-overlay");
    if (!overlay) return;

    const navbarItems = document.querySelectorAll(".navbar__list > .navbar__item");
    let hoverTimeout;
    const HOVER_DELAY = 100;

    navbarItems.forEach((item) => {
        const dropdown = item.querySelector(".dropdown");
        if (!dropdown) return;

        item.addEventListener("mouseenter", () => {
            clearTimeout(hoverTimeout);
            if (window.innerWidth > 991) overlay.classList.add("is-active");
        });

        item.addEventListener("mouseleave", () => {
            hoverTimeout = setTimeout(() => {
                overlay.classList.remove("is-active");
            }, HOVER_DELAY);
        });

        dropdown.addEventListener("mouseenter", () => clearTimeout(hoverTimeout));
        dropdown.addEventListener("mouseleave", () => {
            hoverTimeout = setTimeout(() => overlay.classList.remove("is-active"), HOVER_DELAY);
        });
    });

    document.addEventListener("click", (e) => {
        const isOutsideNavbar = !e.target.closest(".navbar");
        const isInsideOverlay = e.target.id === "menu-overlay" || e.target.classList.contains("menu-overlay");
        if (isOutsideNavbar || isInsideOverlay) {
            if (!e.target.closest(".dropdown")) overlay.classList.remove("is-active");
        }
    });
}

window.addEventListener("template-loaded", setupNavbarOverlay);
document.addEventListener("DOMContentLoaded", setupNavbarOverlay);

window.addEventListener("template-loaded", () => {
    const tabsSelector = "prod-tab__item";
    const contentsSelector = "prod-tab__content";

    const tabActive = `${tabsSelector}--current`;
    const contentActive = `${contentsSelector}--current`;

    const tabContainers = $$(".js-tabs");
    tabContainers.forEach((tabContainer) => {
        const tabs = tabContainer.querySelectorAll(`.${tabsSelector}`);
        const contents = tabContainer.querySelectorAll(`.${contentsSelector}`);
        tabs.forEach((tab, index) => {
            tab.onclick = () => {
                tabContainer
                    .querySelector(`.${tabActive}`)
                    ?.classList.remove(tabActive);
                tabContainer
                    .querySelector(`.${contentActive}`)
                    ?.classList.remove(contentActive);
                tab.classList.add(tabActive);
                contents[index].classList.add(contentActive);
            };
        });
    });
});

window.addEventListener("template-loaded", () => {
    const switchBtn = document.querySelector("#switch-theme-btn");
    if (switchBtn) {
        switchBtn.onclick = function () {
            const isDark = localStorage.dark === "true";
            document.querySelector("html").classList.toggle("dark", !isDark);
            localStorage.setItem("dark", !isDark);
            switchBtn.querySelector("span").textContent = isDark
                ? "Dark mode"
                : "Light mode";
        };
        const isDark = localStorage.dark === "true";
        switchBtn.querySelector("span").textContent = isDark
            ? "Light mode"
            : "Dark mode";
    }
});

const isDark = localStorage.dark === "true";
document.querySelector("html").classList.toggle("dark", isDark);

// Slideshow trang chủ: tự động chuyển slide mỗi 2s, trượt ngang
function initHomeSlideshow() {
    const slideshow = document.querySelector(".js-home-slideshow");
    if (!slideshow) return;

    const inner = slideshow.querySelector(".slideshow__inner");
    const items = inner ? inner.querySelectorAll(".slideshow__item") : [];
    const total = items.length;
    if (!inner || total <= 1) return;
    // Dots manual navigation
    const dots = slideshow.querySelectorAll(".slideshow__dot");
    const dotInputs = slideshow.querySelectorAll(".slideshow__dot-input");

    let index = 0;

    const setActiveDot = (i) => {
        dots.forEach((dot, idx) => dot.classList.toggle("is-active", idx === i));
        const input = slideshow.querySelector(`#home-slide-dot-${i + 1}`);
        if (input) input.checked = true;
    };

    const update = (i) => {
        index = i % total;
        const offset = index * 100;
        inner.style.transform = `translateX(-${offset}%)`;
        setActiveDot(index);
    };

    update(0);

    // Tự động chuyển slide mỗi 2s
    const SLIDE_DELAY = 2000;
    let timer;
    const startTimer = () => {
        timer = setInterval(() => update(index + 1), SLIDE_DELAY);
    };
    const stopTimer = () => {
        if (timer) clearInterval(timer);
    };
    startTimer();

    // Click dot to navigate
    dots.forEach((dot, i) => {
        dot.addEventListener("click", () => {
            stopTimer();
            update(i);
            startTimer();
        });
    });

    // Dừng khi rời trang hoặc template thay đổi
    window.addEventListener("beforeunload", stopTimer);
}

window.addEventListener("template-loaded", initHomeSlideshow);
document.addEventListener("DOMContentLoaded", initHomeSlideshow);
