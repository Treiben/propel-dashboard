import React from 'react';

interface PropelIconProps {
    className?: string;
    size?: number;
}

export const PropelIcon: React.FC<PropelIconProps> = ({ className = '', size = 32 }) => {
    return (
        <img
            src="/propel.png"
            alt="Propel Feature Flags"
            width={size}
            height={size}
            className={`${className} object-contain`}
        />
    );
};