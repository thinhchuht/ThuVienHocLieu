import { useEffect, useState } from "react";
import { Link, NavLink, useLocation } from "react-router";
import { Menu, X } from "lucide-react";
import { cn } from "@/lib/utils";

export interface MobileNavItem {
  to: string;
  label: string;
  end?: boolean;
  /** Mốc phân cách (vd "Quản trị" cuối menu GV). */
  accent?: boolean;
}

/**
 * Menu mobile cho khu GV/Admin (< md): sidebar để ở màn lớn, màn nhỏ thay bằng
 * nút hamburger + drawer trượt (không có menu thì GV không di chuyển được).
 */
export function MobileNav({
  title,
  homeTo,
  items,
}: {
  title: string;
  homeTo: string;
  items: MobileNavItem[];
}) {
  const [open, setOpen] = useState(false);
  const location = useLocation();

  // Đóng drawer khi đã vào trang đích.
  useEffect(() => {
    setOpen(false);
  }, [location.pathname]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open]);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        aria-label="Mở menu"
        className="flex size-10 items-center justify-center rounded-btn text-muted transition hover:bg-paper hover:text-ink md:hidden"
      >
        <Menu className="size-5" />
      </button>
      {open && (
        <div
          className="fixed inset-0 z-50 md:hidden"
          role="dialog"
          aria-modal="true"
          aria-label="Menu điều hướng"
        >
          <button
            type="button"
            aria-label="Đóng menu"
            className="absolute inset-0 cursor-default bg-ink/40"
            onClick={() => setOpen(false)}
          />
          <div className="absolute inset-y-0 left-0 flex w-72 max-w-[85vw] flex-col overflow-y-auto border-r border-grid bg-white p-4">
            <div className="mb-4 flex items-center justify-between">
              <Link to={homeTo} className="flex items-center gap-2">
                <img src="/favicon.svg" alt="" className="size-6" />
                <span className="font-semibold">{title}</span>
              </Link>
              <button
                type="button"
                onClick={() => setOpen(false)}
                aria-label="Đóng menu"
                className="flex size-8 items-center justify-center rounded-btn text-muted transition hover:bg-paper hover:text-ink"
              >
                <X className="size-5" />
              </button>
            </div>
            <nav className="space-y-1">
              {items.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) =>
                    cn(
                      "block rounded-btn px-3 py-2 text-sm text-muted transition hover:bg-paper hover:text-ink",
                      isActive && "bg-violet/10 font-medium text-violet",
                      item.accent &&
                        "mt-2 border-t border-grid pt-3 font-medium text-violet",
                    )
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>
        </div>
      )}
    </>
  );
}
