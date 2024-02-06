'use client';

import {
  IconBrandDiscordFilled,
  IconBrandTelegram,
  IconBrandTwitterFilled,
  IconRocket,
  IconSwords,
  IconTrademark,
} from '@tabler/icons-react';

import Image from 'next/image';
import Link from 'next/link';
import { ReactNode } from 'react';

const links: { label: string; href: string; icon: ReactNode }[] = [
  {
    label: 'Battles',
    href: '/battles',
    icon: <IconSwords />,
  },
  {
    label: 'Discord',
    href: 'https://discord.gg/y7C9tvYh4H',
    icon: <IconBrandDiscordFilled />,
  },
  {
    label: 'Twitter',
    href: 'https://twitter.com/ScoogisNFT',
    icon: <IconBrandTwitterFilled />,
  },
  {
    label: 'Telegram',
    href: 'https://t.me/peensol',
    icon: <IconBrandTelegram />,
  },
  {
    label: 'Buy $PEEN',
    href: 'https://jup.ag/swap/USDC-PEEN_peen77qWZw4XQkvxW1QF6MUJyKNLbqvMzqhkKpB1aVo',
    icon: <IconRocket />,
  },
  {
    label: 'Buy Scoogis',
    href: 'https://www.tensor.trade/trade/scoogis',
    icon: <IconTrademark />,
  },
];

export default function DashboardFeature() {
  return (
    <div>
      <div className="max-w-screen-lg mx-auto py-6 sm:px-6 lg:px-8 text-center flex flex-col items-center">
        <Image src="/hero.png" alt="Scoogi" width={500} height={500} />
        <div className="space-y-2">
          <p>Here are some helpful links to get you started.</p>

          <ul className="menu menu-vertical lg:menu-horizontal bg-base-200 rounded-box">
            {links.map((link, index) => (
              <li key={index}>
                <Link
                  href={link.href}
                  className="link"
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  {link.icon}
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
