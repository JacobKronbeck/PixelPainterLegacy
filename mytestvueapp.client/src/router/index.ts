import { createRouter, createWebHistory } from "vue-router";
import LoginService from "@/services/LoginService";

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: "/",
      name: "Landing",
      component: () => import("../views/LandingPageView.vue")
    },
    {
      path: "/paint",
      name: "New Painter View",
      component: () => import("../views/PainterView.vue")
    },
    {
      path: "/paint/:id",
      name: "Edit Painter View",
      component: () => import("../views/PainterView.vue")
    },
    {
      path: "/new",
      name: "Prompt Painter",
      component: () => import("../views/PromptPainter.vue")
    },
    {
      path: "/gallery",
      name: "Gallery View",
      component: () => import("../views/GalleryView.vue")
    },
    {
      path: "/gallery/location/:location",
      name: "Gallery View Location",
      component: () => import("../views/GalleryView.vue")
    },
    {
      path: "/art/:id",
      name: "Image",
      component: () => import("../views/ImageViewerView.vue")
    },
    {
      path: "/account",
      name: "Account",
      component: () => import("../views/AccountView.vue")
    },
    {
      path: "/thegrid",
      name: "The Grid",
      component: () => import("../views/TheGridView.vue")
    },
    {
      path: "/connect",
      name: "Connect to The Grid",
      component: () => import("../views/GridConnection.vue")
    },
    {
      path: "/notifications",
      name: "Notifications",
      component: () => import("../views/NotificationView.vue")
    },
    {
      path: "/accountpage/:artist",
      name: "AccountPage",
      component: () => import("../views/AccountPage.vue")
    },
    {
      path: "/gallery/tag/:tag",
      name: "TagGallery",
      component: () => import('@/views/GalleryView.vue'),
      props: true
    },
    {
      path: "/map",
      name: "MapViewer",
      component: () => import("../views/MapViewer.vue")
    },
    {
      path: "/mapadd/:id",
      name: "MapPlacer",
      component: () => import("../views/MapPlacer.vue")
    }
    ,
    {
      path: "/admin",
      name: "Admin",
      component: () => import("../views/AdminView.vue"),
      meta: { requiresAdmin: true }
    }
  ]
});

const staleChunkReloadKey = "pixel-painter:stale-chunk-reload";
const dynamicImportError = /dynamically imported module|module script|failed to fetch/i;

router.onError((error, to) => {
  if (!dynamicImportError.test(String(error))) {
    return;
  }

  const destination = to.fullPath || window.location.pathname + window.location.search + window.location.hash;
  if (sessionStorage.getItem(staleChunkReloadKey) === destination) {
    return;
  }

  // An open tab can retain an old Vite entry chunk after Vercel deploys a new
  // build. Reload once so the browser receives the new index and chunk names.
  sessionStorage.setItem(staleChunkReloadKey, destination);
  window.location.assign(destination);
});

router.afterEach((_to, _from, failure) => {
  if (!failure) {
    sessionStorage.removeItem(staleChunkReloadKey);
  }
});
export default router;

// Simple admin guard: routes with meta.requiresAdmin require admin
router.beforeEach(async (to, _from, next) => {
  if (to.meta && (to.meta as any).requiresAdmin) {
    try {
      const isAdmin = await LoginService.getIsAdmin();
      if (!isAdmin) return next({ path: "/" });
    } catch {
      return next({ path: "/" });
    }
  }
  next();
});
