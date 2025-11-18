// Tránh xung đột với BrowserLink/jQuery: không dùng ký hiệu $ toàn cục
const qs = document.querySelector.bind(document);
const qsa = document.querySelectorAll.bind(document);

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
    qs(selector).innerHTML = cached;
  }

  fetch(path)
    .then((res) => res.text())
    .then((html) => {
      if (html !== cached) {
        qs(selector).innerHTML = html;
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
  if (isHidden(qs(".js-dropdown-list"))) return;

  const items = qsa(".js-dropdown-list > li");

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
document.addEventListener("DOMContentLoaded", handleActiveMenu);

function handleActiveMenu() {
  const dropdowns = qsa(".js-dropdown");
  const menus = qsa(".js-menu-list");
  const activeClass = "menu-column__item--active";
  const removeActive = (menu) => {
    const activeItems = menu.querySelectorAll(`.${activeClass}`);
    activeItems.forEach((item) => item.classList.remove(activeClass));
  };
  const removeAllActive = () => {
    menus.forEach((menu) => removeActive(menu));
  };

  // Khởi tạo menu
  const init = () => {
    // Xóa tất cả active trước
    removeAllActive();

    // Không set active mặc định để hiển thị ảnh quảng cáo

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
document.addEventListener("DOMContentLoaded", initJsToggle);

function initJsToggle() {
  qsa(".js-toggle").forEach((button) => {
    const target = button.getAttribute("toggle-target");
    if (!target) {
      document.body.innerText = `Cần thêm toggle-target cho: ${button.outerHTML}`;
    }
    button.onclick = (e) => {
      e.preventDefault();

      if (!qs(target)) {
        return (document.body.innerText = `Không tìm thấy phần tử "${target}"`);
      }
      const isHidden = qs(target).classList.contains("hide");

      requestAnimationFrame(() => {
        qs(target).classList.toggle("hide", !isHidden);
        qs(target).classList.toggle("show", isHidden);
      });
    };
    document.onclick = function (e) {
      if (!e.target.closest(target)) {
        const isHidden = qs(target).classList.contains("hide");
        if (!isHidden) {
          button.click();
        }
      }
    };
  });
}

window.addEventListener("template-loaded", () => {
  const links = qsa(".js-dropdown-list > li > a");

  links.forEach((link) => {
    link.onclick = (e) => {
      if (window.innerWidth > 991) return;
      e.preventDefault();
      const item = link.closest("li");
      item.classList.toggle("navbar__item--active");
    };
  });
});

document.addEventListener("DOMContentLoaded", () => {
  const links = qsa(".js-dropdown-list > li > a");

  links.forEach((link) => {
    link.onclick = (e) => {
      if (window.innerWidth > 991) return;
      e.preventDefault();
      const item = link.closest("li");
      item.classList.toggle("navbar__item--active");
    };
  });
});

function initMobileAccordion() {
  if (window.innerWidth > 991) return;

  qsa(".js-menu-list .menu-column__item > .menu-column__link").forEach(
    (link) => {
      link.onclick = (e) => {
        e.preventDefault();
        const item = link.closest(".menu-column__item");
        const menu = item?.parentElement;
        if (!item || !menu) return;
        Array.from(menu.children).forEach((child) => {
          if (child !== item) child.classList.remove("menu-column__item--active");
        });
        item.classList.toggle("menu-column__item--active");
      };
    }
  );

  qsa(".sub-menu .menu-column .menu-column__heading a").forEach((link) => {
    const activate = (el) => {
      const column = el.closest(".menu-column");
      const wrap = column?.parentElement;
      if (!column || !wrap) return;
      Array.from(wrap.children).forEach((c) => {
        if (c !== column) c.classList.remove("menu-column--active");
      });
      column.classList.toggle("menu-column--active");
    };

    link.onclick = (e) => {
      e.preventDefault();
      activate(link);
    };
  });

  qsa(".sub-menu .menu-column .menu-column__heading").forEach((heading) => {
    heading.onclick = (e) => {
      e.preventDefault();
      activate(heading);
    };
  });
}

window.addEventListener("template-loaded", initMobileAccordion);
document.addEventListener("DOMContentLoaded", initMobileAccordion);

// Bật/tắt overlay mờ nền khi submenu mở
function setupNavbarOverlay() {
  const overlay = document.getElementById("menu-overlay");
  if (!overlay) return;

  const navbar = document.querySelector(".navbar");
  if (!navbar) return;

  let hoverTimeout;
  const HOVER_DELAY = 80;

  // Bật overlay khi hover toàn bộ khu vực navbar (desktop)
  navbar.addEventListener("mouseenter", () => {
    if (window.innerWidth > 991) {
      clearTimeout(hoverTimeout);
      overlay.classList.add("is-active");
    }
  });

  // Tắt overlay khi rời khỏi navbar (desktop)
  navbar.addEventListener("mouseleave", () => {
    hoverTimeout = setTimeout(
      () => overlay.classList.remove("is-active"),
      HOVER_DELAY
    );
  });

  // Click ngoài để tắt overlay
  document.addEventListener("click", (e) => {
    const isOutsideNavbar = !e.target.closest(".navbar");
    const isInsideOverlay =
      e.target.id === "menu-overlay" ||
      e.target.classList.contains("menu-overlay");
    if (isOutsideNavbar || isInsideOverlay) {
      if (!e.target.closest(".dropdown")) overlay.classList.remove("is-active");
    }
  });
}

window.addEventListener("template-loaded", setupNavbarOverlay);
document.addEventListener("DOMContentLoaded", setupNavbarOverlay);

function initTabs() {
  const tabsSelector = "prod-tab__item";
  const contentsSelector = "prod-tab__content";

  const tabActive = `${tabsSelector}--current`;
  const contentActive = `${contentsSelector}--current`;

  const tabContainers = qsa(".js-tabs");
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
}

window.addEventListener("template-loaded", initTabs);
document.addEventListener("DOMContentLoaded", initTabs);

// Preview thumbnails: click to set main image and highlight current
function initProductPreview() {
  const wraps = qsa(".prod-preview");
  wraps.forEach((wrap) => {
    const mainImg = wrap.querySelector(
      ".prod-preview__item .prod-preview__img"
    );
    const thumbsWrap = wrap.querySelector(".prod-preview__thumbs");
    if (!thumbsWrap) return;

    // Event delegation for robustness
    thumbsWrap.addEventListener("click", (e) => {
      const thumb = e.target.closest(".prod-preview__thumb-img");
      if (!thumb) return;
      const src = thumb.dataset.src || thumb.getAttribute("src");
      if (mainImg && src) {
        mainImg.src = src;
        // Reset zoom state when image changes
        mainImg.style.transform = "";
        mainImg.style.transformOrigin = "";
      }
      wrap
        .querySelectorAll(".prod-preview__thumb-img--current")
        .forEach((el) =>
          el.classList.remove("prod-preview__thumb-img--current")
        );
      thumb.classList.add("prod-preview__thumb-img--current");
    });

    // Auto-rotate thumbnails every 5s
    const thumbs = thumbsWrap.querySelectorAll(".prod-preview__thumb-img");
    if (thumbs.length > 1) {
      let currentIndex = Array.from(thumbs).findIndex((t) =>
        t.classList.contains("prod-preview__thumb-img--current")
      );
      if (currentIndex < 0) currentIndex = 0;

      const advance = () => {
        currentIndex = (currentIndex + 1) % thumbs.length;
        const next = thumbs[currentIndex];
        const srcNext = next.dataset.src || next.getAttribute("src");
        if (mainImg && srcNext) {
          mainImg.src = srcNext;
          // Reset zoom on auto-advance
          mainImg.style.transform = "";
          mainImg.style.transformOrigin = "";
        }
        wrap
          .querySelectorAll(".prod-preview__thumb-img--current")
          .forEach((el) => el.classList.remove("prod-preview__thumb-img--current"));
        next.classList.add("prod-preview__thumb-img--current");
      };

      const INTERVAL = 5000;
      const timer = setInterval(advance, INTERVAL);

      // Sync index on user click
      thumbsWrap.addEventListener("click", (e) => {
        const t = e.target.closest(".prod-preview__thumb-img");
        if (!t) return;
        currentIndex = Array.from(thumbs).indexOf(t);
      });

      // Cleanup when leaving page
      window.addEventListener("beforeunload", () => {
        clearInterval(timer);
      });
    }
  });
}

function isLoggedIn() {
  try {
    if (typeof window.isUserLoggedIn === "undefined") return false;
    if (typeof window.isUserLoggedIn === "string") {
      return window.isUserLoggedIn.trim().toLowerCase() === "true";
    }
    return !!window.isUserLoggedIn;
  } catch (_) {
    return false;
  }
}

async function refreshCartPreview() {
  const res = await fetch("/Favorites/CartPreview", { method: "GET", cache: "no-store" });
  if (!res.ok) return;
  const data = await res.json();
  const titleEl = document.getElementById("js-cart-title");
  if (titleEl && typeof data.count === "number") {
    titleEl.textContent = `Bạn có ${data.count} sản phẩm`;
  }
  const subtotalEl = document.getElementById("js-cart-subtotal");
  if (subtotalEl && typeof data.subtotalFormatted === "string") {
    subtotalEl.textContent = data.subtotalFormatted;
  }
  const navQnt = document.getElementById("js-navbar-cart-qnt");
  if (navQnt && typeof data.count === "number") {
    navQnt.textContent = String(data.count);
  }
          const headerSubtotal = document.getElementById("js-header-cart-subtotal");
          if (headerSubtotal && typeof data.subtotalFormatted === "string") {
            headerSubtotal.textContent = data.subtotalFormatted;
          }
  const listEl = document.getElementById("js-cart-dropdown-list");
  if (listEl && Array.isArray(data.items)) {
    if (data.items.length === 0) {
      listEl.innerHTML = '<div class="col"><p class="text-muted">Giỏ hàng của bạn đang trống</p></div>';
      return;
    }
    const html = data.items
      .map(
        (it) =>
          `<div class="col">
            <article class="cart-preview-item">
              <div class="cart-preview-item__img-wrap">
                <a href="/Product/Detail/${it.productId}">
                  <img src="${it.imgUrl}" alt="${it.productName}" class="cart-preview-item__thumb" />
                </a>
              </div>
              <h3 class="cart-preview-item__title">${it.productName}</h3>
              <p class="cart-preview-item__price">${it.priceEachFormatted} x ${it.quantity}</p>
            </article>
          </div>`
      )
      .join("");
    listEl.innerHTML = html;
    try {
      var imgs = listEl.querySelectorAll('img.cart-preview-item__thumb');
      imgs.forEach(function(img){
        img.addEventListener('error', function(){
          img.onerror = null;
          img.src = '/image/default.avif';
        });
      });
    } catch (_) {}
  }
}

window.addEventListener("template-loaded", initProductPreview);
document.addEventListener("DOMContentLoaded", initProductPreview);

// Zoom preview image on hover based on cursor position
function initPreviewZoom() {
  qsa(".prod-preview").forEach((wrap) => {
    const container = wrap.querySelector(".prod-preview__item");
    const img = container ? container.querySelector(".prod-preview__img") : null;
    if (!container || !img) return;

    const ZOOM = 2.2;
    const onEnter = () => {
      container.classList.add("zooming");
    };
    const onMove = (e) => {
      const rect = container.getBoundingClientRect();
      const x = (e.clientX - rect.left) / rect.width;
      const y = (e.clientY - rect.top) / rect.height;
      const ox = Math.max(0, Math.min(1, x)) * 100;
      const oy = Math.max(0, Math.min(1, y)) * 100;
      img.style.transformOrigin = `${ox}% ${oy}%`;
      img.style.transform = `scale(${ZOOM})`;
    };
    const onLeave = () => {
      container.classList.remove("zooming");
      img.style.transform = "";
      img.style.transformOrigin = "";
    };

    container.addEventListener("mouseenter", onEnter);
    container.addEventListener("mousemove", onMove);
    container.addEventListener("mouseleave", onLeave);
  });
}

window.addEventListener("template-loaded", initPreviewZoom);
document.addEventListener("DOMContentLoaded", initPreviewZoom);

window.addEventListener("template-loaded", () => {
  const switchBtn = document.querySelector("#switch-theme-btn");
  if (switchBtn) {
    switchBtn.onclick = function () {
      const isDark = localStorage.dark === "true";
      document.querySelector("html").classList.toggle("dark", !isDark);
      localStorage.setItem("dark", !isDark);
      switchBtn.querySelector("span").textContent = isDark
        ? "Nền tối"
        : "Nền sáng";
    };
    const isDark = localStorage.dark === "true";
    switchBtn.querySelector("span").textContent = isDark
      ? "Nền sáng"
      : "Nền tối";
  }
});

document.addEventListener("DOMContentLoaded", () => {
  const switchBtn = document.querySelector("#switch-theme-btn");
  if (switchBtn) {
    switchBtn.onclick = function () {
      const isDark = localStorage.dark === "true";
      document.querySelector("html").classList.toggle("dark", !isDark);
      localStorage.setItem("dark", !isDark);
      switchBtn.querySelector("span").textContent = isDark
        ? "Nền tối"
        : "Nền sáng";
    };
    const isDark = localStorage.dark === "true";
    switchBtn.querySelector("span").textContent = isDark
      ? "Nền sáng"
      : "Nền tối";
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

// Carousel functionality
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".product-carousel").forEach((carousel) => {
    const inner = carousel.querySelector(".product-carousel-inner");
    const prevButton = carousel.querySelector(".prev-button");
    const nextButton = carousel.querySelector(".next-button");

    if (prevButton) {
      prevButton.addEventListener("click", () => {
        inner.scrollBy({ left: -215, behavior: "smooth" });
      });
    }

    if (nextButton) {
      nextButton.addEventListener("click", () => {
        inner.scrollBy({ left: 215, behavior: "smooth" });
      });
    }

    const updateButtonVisibility = () => {
      if (inner.scrollWidth > inner.clientWidth) {
        prevButton.style.display = "flex";
        nextButton.style.display = "flex";
      } else {
        prevButton.style.display = "none";
        nextButton.style.display = "none";
      }
    };

    // Initial check and update on load
    updateButtonVisibility();

    // Update on window resize
    window.addEventListener("resize", updateButtonVisibility);

    // Update on scroll to hide/show buttons based on scroll position
    inner.addEventListener("scroll", () => {
      if (inner.scrollLeft === 0) {
        prevButton.style.display = "none";
      } else {
        prevButton.style.display = "flex";
      }

      if (inner.scrollLeft + inner.clientWidth >= inner.scrollWidth) {
        nextButton.style.display = "none";
      } else {
        nextButton.style.display = "flex";
      }
    });
  });
});

// Similar products carousel: 12 items total, show 6 per view
function initSimilarCarousel() {
  const carousel = document.getElementById("similarCarousel");
  if (!carousel) return;

  const viewport = carousel.querySelector(".similar-carousel__viewport");
  const track = carousel.querySelector(".similar-carousel__track");
  const items = track ? track.children : [];
  const prevBtn = carousel.querySelector(".similar-carousel__btn--prev");
  const nextBtn = carousel.querySelector(".similar-carousel__btn--next");
  const perPage = 6;
  const total = items.length;
  const pages = Math.max(1, Math.ceil(total / perPage));
  let page = 0;

  function update() {
    if (!viewport || !track) return;
    const width = viewport.clientWidth;
    track.style.transform = `translateX(-${page * width}px)`;
    if (prevBtn) prevBtn.disabled = page === 0;
    if (nextBtn) nextBtn.disabled = page >= pages - 1;
  }

  if (prevBtn) {
    prevBtn.addEventListener("click", () => {
      if (page > 0) {
        page -= 1;
        update();
      }
    });
  }
  if (nextBtn) {
    nextBtn.addEventListener("click", () => {
      if (page < pages - 1) {
        page += 1;
        update();
      }
    });
  }

  window.addEventListener("resize", update);
  update();
}

window.addEventListener("template-loaded", initSimilarCarousel);
document.addEventListener("DOMContentLoaded", initSimilarCarousel);
// Loại bỏ handler cũ gây xung đột và không gọi API
// Favorite toggle logic for product cards
(function () {
  function getAntiForgeryToken() {
    const el = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    );
    return el ? el.value : null;
  }

  function updateHeart(button, liked) {
    const defaultIcon = button.querySelector("img.like-btn__icon.icon");
    const likedIcon = button.querySelector("img.like-btn__icon--liked");
    if (defaultIcon && likedIcon) {
      if (liked) {
        defaultIcon.style.display = "none";
        likedIcon.style.display = "inline-block";
        button.setAttribute("aria-pressed", "true");
        button.dataset.liked = "true";
        button.classList.add("like-btn--liked");
      } else {
        likedIcon.style.display = "none";
        defaultIcon.style.display = "inline-block";
        button.setAttribute("aria-pressed", "false");
        button.dataset.liked = "false";
        button.classList.remove("like-btn--liked");
      }
    }
  }

  document.addEventListener("DOMContentLoaded", function () {
    const buttons = document.querySelectorAll(".js-toggle-favorite");
    buttons.forEach(function (button) {
      // Optional: initialize UI if data-liked is provided
      if (button.dataset.liked) {
        updateHeart(button, button.dataset.liked === "true");
      }

      async function refreshFavoriteDropdown() {
        try {
          const list = document.getElementById("js-favorite-dropdown-list");
          if (!list) return; // Không có dropdown trên trang hiện tại
          const res = await fetch("/Favorites/Recent", {
            method: "GET",
            headers: { Accept: "application/json" },
          });
          const contentType = res.headers.get("content-type") || "";
          if (!res.ok || !contentType.includes("application/json")) return; // Bỏ qua nếu không phải JSON
          const data = await res.json();
          const items = Array.isArray(data.items) ? data.items : [];

          // Xây lại nội dung dropdown
          if (items.length === 0) {
            list.innerHTML = `<div class="col"><p class="text-muted" style="padding: 8px;">Bạn chưa có bất kỳ sản phẩm yêu thích nào</p></div>`;
            return;
          }

          const cols = items
            .map((it) => {
              const href = `/Product/Detail/${it.productId}`;
              return `
              <div class="col">
                <article class="cart-preview-item">
                  <div class="cart-preview-item__img-wrap">
                    <a href="${href}">
                      <img src="${it.imgUrl}" alt="${it.productName}" class="cart-preview-item__thumb" />
                    </a>
                  </div>
                  <h3 class="cart-preview-item__title">${it.productName}</h3>
                  <p class="cart-preview-item__price">${it.priceFormatted} VND</p>
                </article>
              </div>`;
            })
            .join("");

          list.innerHTML = cols;
        } catch (e) {
          console.warn("Không thể làm mới danh sách yêu thích:", e);
        }
      }

      button.addEventListener("click", async function (e) {
        e.preventDefault();

        const variantId = button.dataset.productId || button.dataset.variantId;
        if (!variantId) {
          console.warn(
            "Không tìm thấy VariantId/ProductId trên nút yêu thích."
          );
          return;
        }

        try {
          const headers = { "Content-Type": "application/json" };
          const token = getAntiForgeryToken();
          if (token) headers["RequestVerificationToken"] = token;

          const res = await fetch("/Favorites/ToggleFavorite", {
            method: "POST",
            headers,
            body: JSON.stringify({ variantId: Number(variantId) }),
          });

          const loginRedirectUrl =
            typeof loginUrl === "string" && loginUrl
              ? loginUrl
              : "/Identity/Account/Login";
          const contentType = res.headers.get("content-type") || "";
          if (
            res.status === 401 ||
            res.redirected ||
            !contentType.includes("application/json")
          ) {
            // Nếu server chuyển hướng đến login (302 -> 200 HTML) hoặc trả về 401, chuyển hướng
            window.location.href = loginRedirectUrl;
            return;
          }

          if (!res.ok) throw new Error("Failed to toggle favorite");

          const data = await res.json();
          const likedNow = !!data.isFavorited;
          updateHeart(button, likedNow);

          // Cập nhật số lượng yêu thích ở header nếu có phần tử hiển thị
          try {
            const countEl = document.getElementById("js-favorite-count");
            if (countEl) {
              const current = parseInt(countEl.textContent || "0", 10);
              const next = Math.max(0, current + (likedNow ? 1 : -1));
              countEl.textContent = String(next);
            }
            const countTextEl = document.getElementById(
              "js-favorite-count-text"
            );
            if (countTextEl && countEl) {
              countTextEl.textContent = `Bạn có ${countEl.textContent} mục yêu thích`;
            }
          } catch (e) {
            console.warn(
              "Không thể cập nhật số lượng yêu thích trong header:",
              e
            );
          }

          // Làm mới dropdown top 3 để đồng bộ ngay lập tức
          refreshFavoriteDropdown();

          // Nếu đang ở trang Favorites, khi bỏ yêu thích thì xóa item khỏi DOM ngay
          try {
            if (!likedNow) {
              const article = button.closest("article.cart-item");
              if (article) {
                const list = article.parentElement;
                article.remove();
                // Cập nhật số items
                const desc = document.querySelector(".cart-info__desc");
                if (desc && list) {
                  const count =
                    list.querySelectorAll("article.cart-item").length;
                  desc.textContent = `${count} items`;
                }
                // Nếu hết item, hiển thị trạng thái rỗng
                if (
                  list &&
                  list.querySelectorAll("article.cart-item").length === 0
                ) {
                  const container = document.querySelector(".cart-info__list");
                  if (container) {
                    container.innerHTML = `
                      <div class="alert alert-info favorites-empty" role="alert">
                        <span>Bạn chưa có sản phẩm yêu thích nào.</span>
                      </div>`;
                    // Đảm bảo khu vực nút "Tiếp tục mua sắm" ở đáy hiển thị để giữ vị trí thống nhất
                    const bottom = document.querySelector(".cart-info__bottom");
                    if (bottom) {
                      bottom.style.display = "block";
                    }
                  }
                }
              }
            }
          } catch (e) {
            console.warn("Không thể cập nhật DOM Favorites ngay lập tức:", e);
          }
        } catch (err) {
          console.error("Favorite toggle error:", err);
          alert("Không thể cập nhật trạng thái yêu thích. Vui lòng thử lại!");
        }
      });
    });
  });
})();

// Payment: redirect to Home and show success modal
document.addEventListener("DOMContentLoaded", function () {
  var path = (window.location.pathname || "").toLowerCase();
  if (path.indexOf("/checkout/payment") === -1) return;
  (function(){
    function fmt(n){ try{ return new Intl.NumberFormat('vi-VN').format(n) + ' VND'; }catch(_){ return String(Math.round(n)) + ' VND'; } }
    var main = document.querySelector('main.checkout-page');
    if (!main) return;
    var itemCount = parseInt(main.getAttribute('data-item-count') || '0', 10) || 0;
    var subtotal = parseFloat(main.getAttribute('data-subtotal') || '0') || 0;
    var radios = Array.prototype.slice.call(document.querySelectorAll('.payment-item__checkbox-input'));
    var shippingEl = document.getElementById('js-pay-shipping');
    var totalEl = document.getElementById('js-pay-total');
    var subtotalEl = document.getElementById('js-pay-subtotal');
    var submitBtn = document.getElementById('js-pay-submit-btn');
  var pmRadios = [];
  var cardForm = null;
  var cardHolderEl = null;
  var cardNumberEl = null;
  var cardExpireEl = null;
  var cardCvcEl = null;
    
    function applyCosts(){
      radios.forEach(function(r){
        var rate = parseFloat(r.getAttribute('data-rate') || '0') || 0;
        var wrap = r.closest('.payment-item__checkbox');
        var label = wrap ? wrap.querySelector('.payment-item__cost') : null;
        var lineCount = parseInt(main.getAttribute('data-line-count') || '0', 10) || 0;
        if (label) label.textContent = fmt(rate * lineCount);
      });
    }

    function updateSummary(){
      var sel = radios.find(function(r){ return r.checked; });
      var rate = sel ? parseFloat(sel.getAttribute('data-rate') || '0') || 0 : 0;
      var lineCount = parseInt(main.getAttribute('data-line-count') || '0', 10) || 0;
      var shipping = rate * lineCount;
      if (shippingEl) shippingEl.textContent = fmt(shipping);
      var total = subtotal + shipping;
      if (totalEl) totalEl.textContent = fmt(total);
      if (submitBtn) submitBtn.textContent = 'Thanh toán ' + fmt(total);
      if (subtotalEl && !subtotalEl.textContent) subtotalEl.textContent = fmt(subtotal);
    }

    applyCosts();
    updateSummary();
    radios.forEach(function(r){ r.addEventListener('change', updateSummary); });

  function toggleCardForm(){
    var v = (pmRadios.find(function(r){ return r.checked; })?.value || 'card');
    if (cardForm) cardForm.style.display = v === 'card' ? '' : 'none';
    // Toggle required attributes for card inputs
    var req = v === 'card';
    if (cardHolderEl) cardHolderEl.required = req;
    if (cardNumberEl) cardNumberEl.required = req;
    if (cardExpireEl) cardExpireEl.required = req;
    if (cardCvcEl) cardCvcEl.required = req;
  }
    toggleCardForm();
  })();
    var payBtn = document.querySelector(".checkout-page .cart-info__next-btn");
    if (!payBtn) return;
    payBtn.addEventListener("click", async function (e) {
      e.preventDefault();
      try {
        var mainEl = document.querySelector('main.checkout-page');
        var radiosNow = Array.prototype.slice.call(document.querySelectorAll('.payment-item__checkbox-input'));
        var sel = radiosNow.find(function(r){ return r.checked; });
        var rate = sel ? parseFloat(sel.getAttribute('data-rate') || '0') || 0 : 0;
        var method = rate>=30000? 'express':'standard';
        var addrId = parseInt((mainEl?.getAttribute('data-address-id') || '0'),10) || null;
        var paymentType = 'cod';
        var cardHolder = '';
        var cardNumber = '';
        var cardExpire = '';

        var token = (document.querySelector('input[name="__RequestVerificationToken"]')?.value || '');
        var resp = await fetch('/Checkout/PlaceOrder', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
          credentials: 'include',
          body: JSON.stringify({ addressId: addrId, shippingMethod: method, shippingRate: rate, paymentType: paymentType, cardHolder: cardHolder, cardNumber: cardNumber, expiryDate: cardExpire })
        });
        var data;
        try { data = await resp.json(); } catch(_) { data = null; }
        if (!resp.ok || !data || !data.success) {
          var msg = (data && data.message) ? data.message : 'Không thể tạo đơn hàng. Vui lòng thử lại!';
          alert(msg);
          if (msg.toLowerCase().includes('address') || msg.toLowerCase().includes('địa chỉ')) {
            window.location.href = '/Checkout/Shipping';
          }
          return;
        }
        try { localStorage.setItem('checkoutSuccess', 'true'); } catch (_){ }
        window.location.href = '/';
      } catch (err) {
        console.error('PlaceOrder error:', err);
        alert('Có lỗi xảy ra khi thanh toán.');
      }
    });
  });

// Show checkout success modal on Home
document.addEventListener("DOMContentLoaded", function () {
  if (window.location.pathname !== "/") return;
  var flag = null;
  try { flag = localStorage.getItem("checkoutSuccess"); } catch (_) {}
  if (flag === "true") {
    try { localStorage.removeItem("checkoutSuccess"); } catch (_) {}
    var modal = document.createElement("div");
    modal.className = "modal modal--small show";
    modal.innerHTML = (
      '<div class="modal__overlay"></div>' +
      '<div class="modal__content">' +
      '  <button class="modal__close" aria-label="Đóng">×</button>' +
      '  <h2 class="modal__heading">Thanh toán thành công</h2>' +
      '  <div class="modal__body">' +
      '    <p class="modal__text">Cảm ơn bạn đã mua sắm tại Zenith. Đơn hàng của bạn đang được xử lý và sẽ được giao tới trong thời gian sớm nhất.</p>' +
      '  </div>' +
      '  <div class="modal__bottom">' +
      '    <button class="btn btn--primary">Đóng</button>' +
      '  </div>' +
      '</div>'
    );
    document.body.appendChild(modal);
    var close = function () { modal.classList.remove("show"); setTimeout(function(){ modal.remove(); }, 200); };
    modal.querySelector(".modal__close").addEventListener("click", close);
    modal.querySelector(".modal__overlay").addEventListener("click", close);
    modal.querySelector(".btn--primary").addEventListener("click", close);
  }
});

// Avatar fallback: nếu ảnh bị xóa hoặc lỗi tải, tự động dùng ảnh mặc định
(function () {
    function attachFallback(img) {
        if (!img) return;
        img.addEventListener('error', function onErr() {
            img.onerror = null;
            img.src = '/image/account/default-avatar.jpg';
        });
    }

    function initAvatarFallbacks() {
        document.querySelectorAll('.top-act__avatar, .user-menu__avatar').forEach(attachFallback);
        var profileAvatar = document.getElementById('profileAvatar');
        attachFallback(profileAvatar);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAvatarFallbacks);
    } else {
        initAvatarFallbacks();
    }
})();

// Xử lý tăng/giảm số lượng trong trang Favorites
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".js-qty-minus").forEach((btn) => {
    btn.addEventListener("click", () => {
      const wrap = btn.closest(".cart-item__input");
      const span = wrap ? wrap.querySelector(".js-qty-value") : null;
      if (!span) return;
      const min = parseInt(span.dataset.min || "1", 10);
      const val = Math.max(min, parseInt(span.textContent || "1", 10) - 1);
      span.textContent = String(val);
    });
  });
  document.querySelectorAll(".js-qty-plus").forEach((btn) => {
    btn.addEventListener("click", () => {
      const wrap = btn.closest(".cart-item__input");
      const span = wrap ? wrap.querySelector(".js-qty-value") : null;
      if (!span) return;
      const max = parseInt(span.dataset.max || "99", 10);
      const val = Math.min(max, parseInt(span.textContent || "1", 10) + 1);
      span.textContent = String(val);
    });
  });
});

// Thêm vào giỏ hàng từ trang Favorites
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".js-add-to-cart").forEach((btn) => {
    btn.addEventListener("click", async function () {
      try {
        // Nếu chưa đăng nhập, chuyển sang trang đăng nhập
        if (!isLoggedIn()) {
          if (typeof loginUrl !== "undefined" && loginUrl) {
            window.location.href = loginUrl;
            return;
          }
        }

        const article = btn.closest("article.cart-item");
        if (!article) return;

        // Lấy biến thể đang chọn
        const sel = article.querySelector("select.favorite-variant-select");
        const variantId = sel ? parseInt(sel.value, 10) : null;
        if (!variantId || isNaN(variantId)) {
          alert("Vui lòng chọn biến thể hợp lệ.");
          return;
        }

        // Lấy số lượng
        const qtySpan = article.querySelector(".js-qty-value");
        const quantity = qtySpan ? parseInt(qtySpan.textContent || "1", 10) : 1;
        if (!quantity || quantity < 1) {
          alert("Số lượng không hợp lệ.");
          return;
        }

        const resp = await fetch("/Favorites/AddToCart", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ VariantId: variantId, Quantity: quantity }),
        });

        if (!resp.ok) {
          const txt = await resp.text();
          console.error("AddToCart failed:", txt);
          alert("Không thể thêm vào giỏ hàng. Vui lòng thử lại!");
          return;
        }

        const data = await resp.json();
        if (data && data.success) {
          try { await refreshCartPreview(); } catch (_) {}
          try { await refreshCartTotals(); } catch (_) {}
        } else {
          alert(data?.message || "Không thể thêm vào giỏ hàng.");
        }
      } catch (e) {
        console.error("Add to cart error:", e);
        alert("Có lỗi xảy ra khi thêm vào giỏ.");
      }
    });
  });
});

// Xử lý đổi variant trong trang Favorites
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".favorite-variant-select").forEach((sel) => {
    sel.addEventListener("change", async function () {
      try {
        const newId = parseInt(sel.value, 10);
        const article = sel.closest("article.cart-item");
        if (!article || !newId) return;
        const likeBtn = article.querySelector(".js-toggle-favorite");
        const oldId = likeBtn
          ? parseInt(likeBtn.dataset.variantId || "0", 10)
          : 0;
        const headers = { "Content-Type": "application/json" };
        const token = document.querySelector(
          'input[name="__RequestVerificationToken"]'
        );
        if (token) headers["RequestVerificationToken"] = token.value;
        const res = await fetch("/Favorites/ChangeVariant", {
          method: "POST",
          headers,
          body: JSON.stringify({ oldVariantId: oldId, newVariantId: newId }),
        });
        if (!res.ok) throw new Error("ChangeVariant failed");
        const data = await res.json();

        // Cập nhật UI: giá, tồn kho, ảnh và dataset
        const priceWrap = article.querySelector(".cart-item__price-wrap");
        const totalPrice = article.querySelector(".cart-item__total-price");
        if (priceWrap) {
          const statusSpan = priceWrap.querySelector(".cart-item__status");
          priceWrap.innerHTML = `${
            data.priceFormatted
          } | <span class="cart-item__status">${
            data.stockQuantity > 0 ? "Còn hàng" : "Hết hàng"
          }</span>`;
        }
        if (totalPrice) totalPrice.textContent = data.priceFormatted;
        if (likeBtn) {
          likeBtn.dataset.variantId = String(data.newVariantId);
          likeBtn.dataset.productId = String(data.newVariantId);
        }
      } catch (e) {
        console.error("Đổi variant yêu thích lỗi:", e);
        alert("Không thể đổi biến thể. Vui lòng thử lại!");
      }
    });
  });
});

// Cho phép click toàn bộ box .cart-item__input mở dropdown biến thể
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".cart-item__input").forEach((box) => {
    const sel = box.querySelector("select.favorite-variant-select");
    if (!sel) return; // Chỉ áp dụng cho box chứa select biến thể

    const openPicker = () => {
      try {
        if (typeof sel.showPicker === "function") {
          sel.showPicker();
          return;
        }
      } catch (_) {
        /* ignore */
      }
      sel.focus();
      sel.click();
    };

    // Click ở bất kỳ chỗ nào trong box sẽ mở dropdown
    box.addEventListener("click", (e) => {
      if (
        e.target &&
        (e.target.tagName === "SELECT" ||
          e.target.closest("select.favorite-variant-select"))
      ) {
        return; // Để mặc định nếu click trực tiếp vào select
      }
      openPicker();
    });

    // Hỗ trợ bàn phím: Enter/Space
    box.addEventListener("keydown", (e) => {
      if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        openPicker();
      }
    });

    // Làm cho box có thể focus bằng bàn phím
    if (!box.hasAttribute("tabindex")) {
      box.setAttribute("tabindex", "0");
    }
  });
});

// Trang Chi tiết sản phẩm: cập nhật giá theo số lượng
document.addEventListener("DOMContentLoaded", function () {
  const root = document.querySelector(".product-page");
  if (!root) return;

  const qtySpan = root.querySelector(".js-qty-value");
  const priceEl = root.querySelector(".prod-info__price");
  const oldPriceEl = root.querySelector(".prod-info__price--old");

  if (!qtySpan || !priceEl) return;

  const formatCurrencyVND = (n) => {
    try {
      return (
        new Intl.NumberFormat("vi-VN", { maximumFractionDigits: 0 }).format(
          Math.round(n)
        ) + " VND"
      );
    } catch (_) {
      return Math.round(n).toString() + " VND";
    }
  };

  const updateTotals = () => {
    const qty = parseInt(qtySpan.textContent || "1", 10) || 1;
    const unitSale = priceEl.dataset.unitSalePrice
      ? parseFloat(priceEl.dataset.unitSalePrice)
      : null;
    const unitPrice = priceEl.dataset.unitPrice
      ? parseFloat(priceEl.dataset.unitPrice)
      : null;
    const unitOld = oldPriceEl && oldPriceEl.dataset.unitPrice
      ? parseFloat(oldPriceEl.dataset.unitPrice)
      : null;

    if (!isNaN(unitSale) && !isNaN(unitOld) && oldPriceEl) {
      oldPriceEl.textContent = formatCurrencyVND(unitOld * qty);
      priceEl.textContent = formatCurrencyVND(unitSale * qty);
    } else if (!isNaN(unitPrice)) {
      priceEl.textContent = formatCurrencyVND(unitPrice * qty);
    }
  };

  // Quan sát thay đổi text số lượng để cập nhật giá
  const observer = new MutationObserver(() => updateTotals());
  observer.observe(qtySpan, { childList: true, subtree: true, characterData: true });

  // Cập nhật sau khi ấn nút +/-
  root.addEventListener("click", (e) => {
    const target = e.target.closest(".js-qty-minus, .js-qty-plus");
    if (target) setTimeout(updateTotals, 0);
  });

  // Khởi tạo
  updateTotals();
});

// Trang Chi tiết sản phẩm: thêm vào giỏ
document.addEventListener("DOMContentLoaded", function () {
  const root = document.querySelector(".product-page");
  if (!root) return;

  const addBtn = root.querySelector(".prod-info__add-to-cart");
  if (!addBtn) return;

  addBtn.addEventListener("click", async function (e) {
    e.preventDefault();
    try {
      if (!isLoggedIn()) {
        if (typeof loginUrl !== "undefined" && loginUrl) {
          window.location.href = loginUrl;
          return;
        }
      }

      const qtySpan = root.querySelector(".js-qty-value");
      const quantity = qtySpan ? parseInt(qtySpan.textContent || "1", 10) : 1;
      if (!quantity || quantity < 1) return;

      let variantId = null;
      const sel = root.querySelector('select[data-variant-select="true"]');
      if (sel) {
        variantId = parseInt(sel.value || "0", 10);
      } else {
        const productId = parseInt(root.getAttribute("data-product-id") || "0", 10);
        const selects = root.querySelectorAll("select[data-attr-id]");
        const valueIds = Array.from(selects)
          .map((s) => parseInt(s.value || "0", 10))
          .filter((x) => !!x);
        if (productId && valueIds.length > 0) {
          const res = await fetch("/Product/ResolveVariant", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ ProductId: productId, ValueIds: valueIds }),
          });
          if (res.ok) {
            const data = await res.json();
            if (data && data.success) {
              variantId = parseInt(data.variantId || "0", 10);
            }
          }
        }
      }

      if (!variantId || isNaN(variantId) || variantId <= 0) {
        const likeBtn = root.querySelector(".js-toggle-favorite");
        if (likeBtn) {
          variantId = parseInt(likeBtn.dataset.variantId || "0", 10);
        }
      }

      if (!variantId || isNaN(variantId) || variantId <= 0) {
        alert("Vui lòng chọn biến thể hợp lệ.");
        return;
      }

      const resp = await fetch("/Favorites/AddToCart", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ VariantId: variantId, Quantity: quantity }),
      });
      if (!resp.ok) {
        const txt = await resp.text();
        console.error("AddToCart failed:", txt);
        alert("Không thể thêm vào giỏ hàng. Vui lòng thử lại!");
        return;
      }
      const data = await resp.json();
      if (data && data.success) {
        try { await refreshCartPreview(); } catch (_) {}
        try { await refreshCartTotals(); } catch (_) {}
      } else {
        alert(data?.message || "Không thể thêm vào giỏ hàng.");
      }
    } catch (err) {
      console.error("Add to cart (detail) error:", err);
      alert("Có lỗi xảy ra khi thêm vào giỏ.");
    }
  });
});

// Profile: thêm vào giỏ từ danh sách yêu thích
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".js-add-to-cart-profile").forEach((btn) => {
    btn.addEventListener("click", async function () {
      try {
        if (!isLoggedIn()) {
          if (typeof loginUrl !== "undefined" && loginUrl) {
            window.location.href = loginUrl;
            return;
          }
        }

        const variantId = parseInt(btn.dataset.variantId || "0", 10);
        const quantity = 1;
        if (!variantId || isNaN(variantId) || variantId <= 0) {
          alert("Không xác định được biến thể.");
          return;
        }

        const resp = await fetch("/Favorites/MoveToCart", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ VariantId: variantId, Quantity: quantity }),
        });
        if (!resp.ok) {
          const txt = await resp.text();
          console.error("MoveToCart failed:", txt);
          alert("Không thể chuyển sang giỏ hàng. Vui lòng thử lại!");
          return;
        }
        const data = await resp.json();
        if (data && data.success) {
          try { await refreshCartPreview(); } catch (_) {}
          try { await refreshCartTotals(); } catch (_) {}
          try {
            const item = btn.closest("article.favourite-item");
            if (item) {
              const sep = item.nextElementSibling;
              item.remove();
              if (sep && sep.classList && sep.classList.contains("separate")) {
                sep.remove();
              }
            }
          } catch (_) {}
        } else {
          alert(data?.message || "Không thể chuyển sang giỏ hàng.");
        }
      } catch (e) {
        console.error("Move to cart (profile) error:", e);
        alert("Có lỗi xảy ra khi chuyển sang giỏ.");
      }
    });
  });
});

// Filter accordion toggles on Product Index
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".filter-sport-parent").forEach((btn) => {
    btn.addEventListener("click", () => {
      const li = btn.closest(".filter-node");
      if (!li) return;
      li.classList.toggle("open");
    });
  });
  document.querySelectorAll(".filter-sport-child").forEach((btn) => {
    btn.addEventListener("click", () => {
      const li = btn.closest(".filter-node");
      if (!li) return;
      li.classList.toggle("open");
    });
  });
});

// Header search: use external icon button to focus/submit
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".js-header-search-btn").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var group = btn.closest(".top-act__group--search");
      if (!group) return;
      var form = group.querySelector(".header-search");
      var input = group.querySelector(".header-search__input");
      if (!form || !input) return;
      var isOpen = form.classList.contains("header-search--open");
      if (!isOpen) {
        form.classList.add("header-search--open");
        input.focus();
        return;
      }
      var val = (input.value || "").trim();
      if (val.length > 0) {
        form.submit();
      } else {
        input.focus();
      }
    });
  });
});

// Close header search when clicking outside or pressing Escape (empty only)
document.addEventListener("DOMContentLoaded", function () {
  document.addEventListener("click", function (e) {
    document.querySelectorAll(".top-act__group--search").forEach(function (group) {
      var form = group.querySelector(".header-search");
      var input = group.querySelector(".header-search__input");
      if (!form) return;
      var inside = !!e.target.closest(".top-act__group--search");
      if (!inside && form.classList.contains("header-search--open")) {
        var val = (input && input.value || "").trim();
        if (!val) form.classList.remove("header-search--open");
      }
    });
  });
  document.querySelectorAll(".header-search__input").forEach(function (input) {
    input.addEventListener("keydown", function (e) {
      if (e.key === "Escape") {
        var form = input.closest(".header-search");
        if (form && !input.value.trim()) form.classList.remove("header-search--open");
      }
    });
  });
});

// Search bar navigation: redirect to Product Index with query
document.addEventListener("DOMContentLoaded", function () {
  document.querySelectorAll(".search-bar").forEach((bar) => {
    const input = bar.querySelector(".search-bar__input");
    const submit = bar.querySelector(".search-bar__submit");
    const go = () => {
      const q = input?.value?.trim();
      const url = q ? `/Product?q=${encodeURIComponent(q)}` : `/Product`;
      window.location.href = url;
    };
    if (submit) submit.addEventListener("click", (e) => { e.preventDefault(); go(); });
    if (input) input.addEventListener("keydown", (e) => { if (e.key === "Enter") { e.preventDefault(); go(); } });
  });
});

async function refreshCartTotals() {
  try {
    const res = await fetch("/Checkout/GetCartSummary", { method: "GET", cache: "no-store" });
    if (!res.ok) return;
    const data = await res.json();
    const subtotalEl = document.getElementById("js-cart-subtotal");
    if (subtotalEl && typeof data.subtotalFormatted === "string") {
      subtotalEl.textContent = data.subtotalFormatted;
    }
    const shippingEl = document.getElementById("js-cart-shipping");
    if (shippingEl && typeof data.shippingFormatted === "string") {
      shippingEl.textContent = data.shippingFormatted;
    }
    const totalEl = document.getElementById("js-cart-total");
    if (totalEl && typeof data.totalFormatted === "string") {
      totalEl.textContent = data.totalFormatted;
    }
    const headerSubtotal = document.getElementById("js-header-cart-subtotal");
    if (headerSubtotal && typeof data.subtotalFormatted === "string") {
      headerSubtotal.textContent = data.subtotalFormatted;
    }
  } catch (_) {
  }
}
